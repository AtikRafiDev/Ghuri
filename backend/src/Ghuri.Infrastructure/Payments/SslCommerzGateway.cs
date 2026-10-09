using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ghuri.Infrastructure.Payments;

/// <summary>
/// SSLCommerz (bKash, Nagad, cards…) behind IPaymentGateway - 17-day plan,
/// Day 9: "SslCommerzGateway (sandbox session)", Day 10: the validation API,
/// then the refund API (send money back, ask how it's going). API v4:
/// https://developer.sslcommerz.com/doc/v4/
/// </summary>
/// <remarks>
/// <para>
/// Session = one form POST to /gwprocess/v4/api.php. SSLCommerz answers JSON:
/// status SUCCESS + GatewayPageURL (where the customer pays) + sessionkey,
/// or status FAILED + failedreason.
/// </para>
/// <para>
/// The store password travels in the form, so the form is NEVER logged -
/// only our transaction id, the outcome and SSLCommerz's reason.
/// </para>
/// </remarks>
internal sealed class SslCommerzGateway(HttpClient http, IOptions<SslCommerzOptions> options, ILogger<SslCommerzGateway> logger)
    : IPaymentGateway
{
    // Where the customer's browser is sent back to - PaymentReturnController (Part 3).
    public const string SuccessPath = "/api/v1/payments/sslcommerz/success";
    public const string FailPath = "/api/v1/payments/sslcommerz/fail";
    public const string CancelPath = "/api/v1/payments/sslcommerz/cancel";

    public PaymentProvider Provider => PaymentProvider.SslCommerz;

    public async Task<PaymentSessionResult> CreateSessionAsync(PaymentSessionRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var callbackBase = settings.CallbackBaseUrl.TrimEnd('/');

        var form = new Dictionary<string, string>
        {
            ["store_id"] = settings.StoreId,
            ["store_passwd"] = settings.StorePassword,
            // Two decimals with a dot, whatever the server's language settings ("16000.00").
            ["total_amount"] = request.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            ["currency"] = request.Currency,
            ["tran_id"] = request.TransactionId,
            ["success_url"] = callbackBase + SuccessPath,
            ["fail_url"] = callbackBase + FailPath,
            ["cancel_url"] = callbackBase + CancelPath,
            ["cus_name"] = request.CustomerName,
            ["cus_email"] = request.CustomerEmail,
            ["cus_phone"] = request.CustomerPhone,
            ["cus_add1"] = settings.CustomerAddress,
            ["cus_city"] = settings.CustomerCity,
            ["cus_postcode"] = settings.CustomerPostcode,
            ["cus_country"] = settings.CustomerCountry,
            ["shipping_method"] = "NO", // nothing is shipped - so no ship_* fields are needed
            ["product_name"] = Truncate(request.ProductName, 255),
            ["product_category"] = settings.ProductCategory,
            ["product_profile"] = settings.ProductProfile,
            // Echoed back with every message about this payment - handy when reconciling.
            ["value_a"] = request.Reference,
        };
        if (!string.IsNullOrWhiteSpace(settings.IpnUrl))
            form["ipn_url"] = settings.IpnUrl;

        try
        {
            using var response = await http.PostAsync(
                new Uri(settings.BaseUrl.TrimEnd('/') + "/gwprocess/v4/api.php"),
                new FormUrlEncodedContent(form),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Refused(request, $"SSLCommerz answered HTTP {(int)response.StatusCode}.");

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var answer = JsonSerializer.Deserialize<SessionAnswer>(body);

            if (answer is { Status: "SUCCESS", GatewayPageUrl: { Length: > 0 } pageUrl, SessionKey: { Length: > 0 } sessionKey })
            {
                logger.LogInformation("SSLCommerz session created for {TransactionId}.", request.TransactionId);
                return PaymentSessionResult.Success(pageUrl, sessionKey);
            }

            return Refused(request, string.IsNullOrWhiteSpace(answer?.FailedReason)
                ? "SSLCommerz refused the payment session."
                : answer.FailedReason);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
                                          || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // Unreachable, timed out (TaskCanceled while OUR token is fine =
            // the HttpClient timeout), or an answer that isn't JSON.
            logger.LogWarning(exception, "Could not create an SSLCommerz session for {TransactionId}.", request.TransactionId);
            return PaymentSessionResult.Failure("The payment service could not be reached. Please try again in a moment.");
        }
    }

    /// <summary>
    /// Day 10: "is this payment real?" - GET /validator/api/validationserverAPI.php
    /// with the val_id from SSLCommerz's message. Answers status VALID (or
    /// VALIDATED = already asked before, still a real payment) with the
    /// transaction's details, or INVALID_TRANSACTION.
    /// </summary>
    /// <remarks>
    /// The store password travels in the query string, so the URL is never
    /// logged here (.NET's own HttpClient logging hides query strings).
    /// Amounts arrive as text ("34000.00"), sometimes as numbers - both are read.
    /// </remarks>
    public async Task<PaymentValidationResult> ValidatePaymentAsync(string validationId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(validationId);
        var settings = options.Value;
        var url = settings.BaseUrl.TrimEnd('/') + "/validator/api/validationserverAPI.php"
                  + "?val_id=" + Uri.EscapeDataString(validationId)
                  + "&store_id=" + Uri.EscapeDataString(settings.StoreId)
                  + "&store_passwd=" + Uri.EscapeDataString(settings.StorePassword)
                  + "&v=1&format=json";

        try
        {
            using var response = await http.GetAsync(new Uri(url), cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Unreachable(validationId, $"SSLCommerz answered HTTP {(int)response.StatusCode}.");

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var answer = json.RootElement;
            var status = Text(answer, "status");

            if (status is not ("VALID" or "VALIDATED"))
            {
                logger.LogWarning("SSLCommerz says val_id {ValidationId} is not a valid payment: {Status}.", validationId, status);
                return PaymentValidationResult.Invalid($"SSLCommerz says: {status ?? "no status"}.");
            }

            // currency_type / currency_amount = what WE asked for; currency /
            // amount = what was settled in. The same for taka, but compare
            // against what we asked for.
            var currency = Text(answer, "currency_type") ?? Text(answer, "currency");
            var amount = Number(answer, "currency_amount") ?? Number(answer, "amount");
            var transactionId = Text(answer, "tran_id");
            if (currency is null || amount is null || transactionId is null)
                return PaymentValidationResult.Invalid("SSLCommerz's answer is missing the transaction, amount or currency.");

            var fee = Number(answer, "amount") - Number(answer, "store_amount"); // null if either is missing
            var isHighRisk = Text(answer, "risk_level") == "1";
            if (isHighRisk)
                logger.LogWarning("SSLCommerz marks payment {TransactionId} as high risk: {RiskTitle}.", transactionId, Text(answer, "risk_title"));

            logger.LogInformation("SSLCommerz validated payment {TransactionId}.", transactionId);
            return PaymentValidationResult.Valid(
                transactionId, amount.Value, currency, Text(answer, "card_type"), Text(answer, "bank_tran_id"), fee, isHighRisk);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
                                          || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(exception, "Could not validate SSLCommerz val_id {ValidationId}.", validationId);
            return PaymentValidationResult.Unreachable("The payment service could not be reached.");
        }
    }

    /// <summary>
    /// "Send this money back" - GET /validator/api/merchantTransIDvalidationAPI.php
    /// with the payment's bank_tran_id. SSLCommerz answers APIConnect DONE +
    /// status "success" (accepted) and a refund_ref_id to ask about later.
    /// https://developer.sslcommerz.com/doc/v4/#refund-section
    /// </summary>
    /// <remarks>
    /// <para>
    /// refund_trans_id (required since 24 Feb 2025) is our RefundNo, the same
    /// on every try: SSLCommerz recognises a repeat of the same refund. A
    /// timeout leaves us not knowing whether it arrived, so a retry must never
    /// look like a second refund.
    /// </para>
    /// <para>
    /// status "processing" = a refund for this payment was ALREADY started
    /// (e.g. we were told "success" but couldn't save it). With its
    /// refund_ref_id that's the same as started. Without one it is Unconfirmed,
    /// never Refused: staff might then send the money by hand while
    /// SSLCommerz sends it too.
    /// </para>
    /// <para>
    /// LIVE refunds only work from a public IP registered with SSLCommerz
    /// (ask their support); the sandbox accepts any IP.
    /// </para>
    /// </remarks>
    public async Task<RefundStartResult> StartRefundAsync(RefundStartRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var settings = options.Value;
        var url = RefundApiUrl(settings)
                  + "?bank_tran_id=" + Uri.EscapeDataString(request.ProviderTransactionId)
                  + "&refund_trans_id=" + Uri.EscapeDataString(request.RefundId)
                  // Two decimals with a dot, whatever the server's language settings ("17000.00").
                  + "&refund_amount=" + request.Amount.ToString("0.00", CultureInfo.InvariantCulture)
                  + "&refund_remarks=" + Uri.EscapeDataString(Truncate(request.Reason, 255))
                  + "&refe_id=" + Uri.EscapeDataString(Truncate(request.Reference, 50))
                  + "&store_id=" + Uri.EscapeDataString(settings.StoreId)
                  + "&store_passwd=" + Uri.EscapeDataString(settings.StorePassword)
                  + "&v=1&format=json";

        try
        {
            using var response = await http.GetAsync(new Uri(url), cancellationToken);
            if (!response.IsSuccessStatusCode)
                return RefundUnconfirmed(request.RefundId, $"SSLCommerz answered HTTP {(int)response.StatusCode}.");

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var answer = json.RootElement;
            var apiConnect = Text(answer, "APIConnect");
            var status = Text(answer, "status");
            var refundRefId = Text(answer, "refund_ref_id");
            var errorReason = Text(answer, "errorReason");

            // INVALID_REQUEST / FAILED (wrong store id or password) / INACTIVE (store switched off): nothing was started.
            if (apiConnect != "DONE")
                return RefundRefused(request.RefundId, $"SSLCommerz refused the request ({apiConnect ?? "no answer"}){Because(errorReason)}");

            switch (status)
            {
                case "success" or "processing" when refundRefId is not null:
                    logger.LogInformation("SSLCommerz accepted refund {RefundId} ({Status}): refund_ref_id {RefundRefId}.", request.RefundId, status, refundRefId);
                    return RefundStartResult.Started(refundRefId);
                case "processing":
                    return RefundUnconfirmed(request.RefundId,
                        "SSLCommerz says a refund for this payment is already in progress, but didn't say which. Check the SSLCommerz merchant panel.");
                case "failed":
                    return RefundRefused(request.RefundId, $"SSLCommerz refused the refund{Because(errorReason)}");
                default:
                    return RefundUnconfirmed(request.RefundId, $"SSLCommerz gave an answer we don't understand (status {status ?? "missing"}).");
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
                                          || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(exception, "Could not reach SSLCommerz to start refund {RefundId}.", request.RefundId);
            return RefundStartResult.Unconfirmed("SSLCommerz could not be reached. Please try again in a moment.");
        }
    }

    /// <summary>
    /// "How is this refund going?" - the same address, with the refund_ref_id
    /// it gave us. status: refunded · processing · cancelled. The sandbox also
    /// answers "failed" + errorReason (e.g. "Unknown Refund Ref ID"), which
    /// the documentation doesn't list: no money was sent either way.
    /// </summary>
    public async Task<RefundStatusResult> GetRefundStatusAsync(string providerRefundId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerRefundId);
        var settings = options.Value;
        var url = RefundApiUrl(settings)
                  + "?refund_ref_id=" + Uri.EscapeDataString(providerRefundId)
                  + "&store_id=" + Uri.EscapeDataString(settings.StoreId)
                  + "&store_passwd=" + Uri.EscapeDataString(settings.StorePassword)
                  + "&format=json";

        try
        {
            using var response = await http.GetAsync(new Uri(url), cancellationToken);
            if (!response.IsSuccessStatusCode)
                return RefundStatusUnconfirmed(providerRefundId, $"SSLCommerz answered HTTP {(int)response.StatusCode}.");

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var answer = json.RootElement;
            var apiConnect = Text(answer, "APIConnect");
            var status = Text(answer, "status");
            var errorReason = Text(answer, "errorReason");

            if (apiConnect != "DONE")
                return RefundStatusUnconfirmed(providerRefundId, $"SSLCommerz didn't answer ({apiConnect ?? "no answer"}){Because(errorReason)}");

            switch (status)
            {
                case "refunded":
                    logger.LogInformation("SSLCommerz says refund {RefundRefId} is done.", providerRefundId);
                    return RefundStatusResult.Refunded;
                case "processing":
                    return RefundStatusResult.Processing;
                case "cancelled" or "failed":
                    logger.LogWarning("SSLCommerz says refund {RefundRefId} {Status}: {Reason}", providerRefundId, status, errorReason);
                    return RefundStatusResult.Failed($"SSLCommerz {(status == "cancelled" ? "cancelled" : "failed")} the refund{Because(errorReason)}");
                default:
                    return RefundStatusUnconfirmed(providerRefundId, $"SSLCommerz gave an answer we don't understand (status {status ?? "missing"}).");
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
                                          || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(exception, "Could not reach SSLCommerz to check refund {RefundRefId}.", providerRefundId);
            return RefundStatusResult.Unconfirmed("SSLCommerz could not be reached.");
        }
    }

    private static string RefundApiUrl(SslCommerzOptions settings) =>
        settings.BaseUrl.TrimEnd('/') + "/validator/api/merchantTransIDvalidationAPI.php";

    /// <summary>": Invalid bank tran id." - or just "." when SSLCommerz gave no reason.</summary>
    private static string Because(string? reason) => string.IsNullOrWhiteSpace(reason) ? "." : $": {reason.TrimEnd('.')}.";

    private RefundStartResult RefundRefused(string refundId, string reason)
    {
        logger.LogWarning("SSLCommerz refused refund {RefundId}: {Reason}", refundId, reason);
        return RefundStartResult.Refused(reason);
    }

    private RefundStartResult RefundUnconfirmed(string refundId, string reason)
    {
        logger.LogWarning("Refund {RefundId} not confirmed by SSLCommerz: {Reason}", refundId, reason);
        return RefundStartResult.Unconfirmed(reason);
    }

    private RefundStatusResult RefundStatusUnconfirmed(string providerRefundId, string reason)
    {
        logger.LogWarning("Could not check SSLCommerz refund {RefundRefId}: {Reason}", providerRefundId, reason);
        return RefundStatusResult.Unconfirmed(reason);
    }

    private PaymentValidationResult Unreachable(string validationId, string reason)
    {
        logger.LogWarning("Could not validate SSLCommerz val_id {ValidationId}: {Reason}", validationId, reason);
        return PaymentValidationResult.Unreachable(reason);
    }

    /// <summary>A field as text, whether SSLCommerz sent it as a string or a number. Null if missing or empty.</summary>
    private static string? Text(JsonElement answer, string name) =>
        answer.ValueKind == JsonValueKind.Object && answer.TryGetProperty(name, out var value)
            ? value.ValueKind switch
            {
                JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()) ? null : value.GetString()!.Trim(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null
            }
            : null;

    private static decimal? Number(JsonElement answer, string name) =>
        decimal.TryParse(Text(answer, name), NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : null;

    private PaymentSessionResult Refused(PaymentSessionRequest request, string reason)
    {
        logger.LogWarning("SSLCommerz refused a session for {TransactionId}: {Reason}", request.TransactionId, reason);
        return PaymentSessionResult.Failure(reason);
    }

    private static string Truncate(string text, int maxLength) => text.Length <= maxLength ? text : text[..maxLength];

    /// <summary>The parts of SSLCommerz's answer we use (it also lists gateways, logos…).</summary>
    private sealed record SessionAnswer(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("failedreason")] string? FailedReason,
        [property: JsonPropertyName("sessionkey")] string? SessionKey,
        [property: JsonPropertyName("GatewayPageURL")] string? GatewayPageUrl);
}
