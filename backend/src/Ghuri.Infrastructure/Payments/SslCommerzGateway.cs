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
/// Day 9: "SslCommerzGateway (sandbox session)". API v4:
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
