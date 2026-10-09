using System.Net;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Infrastructure.Payments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ghuri.Api.IntegrationTests.Payments;

/// <summary>
/// SslCommerzGateway without the internet: a fake HTTP handler records what
/// would be sent and plays back SSLCommerz's answer. Money: the amount must
/// be exact, and the store password must never reach the logs.
/// </summary>
public class SslCommerzGatewayTests
{
    private const string StorePassword = "secret-store-password@ssl";

    private static readonly PaymentSessionRequest Request = new(
        TransactionId: "PAY100001", Reference: "TB100001", Amount: 34_000m, Currency: "BDT",
        ProductName: "Beach Escape - 20 Dec 2026", CustomerName: "Rahim Uddin",
        CustomerEmail: "rahim@example.com", CustomerPhone: "01712345678");

    private const string SuccessJson =
        """{"status":"SUCCESS","failedreason":"","sessionkey":"F2A7C1E3B9D4","GatewayPageURL":"https://sandbox.sslcommerz.com/EasyCheckOut/testcdeF2A7C1E3B9D4","gw":{}}""";

    private static (SslCommerzGateway Gateway, FakeHandler Handler, ListLogger Logger) Create(
        HttpResponseMessage? answer = null, Exception? throws = null, bool sandbox = true, string? ipnUrl = null)
    {
        var handler = new FakeHandler(answer ?? Json(SuccessJson), throws);
        var logger = new ListLogger();
        var options = Options.Create(new SslCommerzOptions
        {
            StoreId = "ghuritest123",
            StorePassword = StorePassword,
            UseSandbox = sandbox,
            CallbackBaseUrl = "http://localhost:5173/",
            IpnUrl = ipnUrl
        });
        return (new SslCommerzGateway(new HttpClient(handler), options, logger), handler, logger);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

    [Fact]
    public async Task SendsTheFormSslCommerzExpects()
    {
        var (gateway, handler, _) = Create();

        await gateway.CreateSessionAsync(Request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://sandbox.sslcommerz.com/gwprocess/v4/api.php", handler.Url);
        var form = handler.Form;
        Assert.Equal("ghuritest123", form["store_id"]);
        Assert.Equal("34000.00", form["total_amount"]); // two decimals, a dot - never "34000" or "34.000,00"
        Assert.Equal(("BDT", "PAY100001", "TB100001"), (form["currency"], form["tran_id"], form["value_a"]));
        Assert.Equal("http://localhost:5173/api/v1/payments/sslcommerz/success", form["success_url"]);
        Assert.Equal("http://localhost:5173/api/v1/payments/sslcommerz/fail", form["fail_url"]);
        Assert.Equal("http://localhost:5173/api/v1/payments/sslcommerz/cancel", form["cancel_url"]);
        Assert.Equal(("NO", "non-physical-goods"), (form["shipping_method"], form["product_profile"]));
        Assert.Equal(("rahim@example.com", "01712345678", "Dhaka"), (form["cus_email"], form["cus_phone"], form["cus_city"]));
        Assert.False(form.ContainsKey("ipn_url")); // not configured locally → not sent
    }

    [Fact]
    public async Task AnIpnUrl_IsSentWhenConfigured()
    {
        var (gateway, handler, _) = Create(ipnUrl: "https://www.ghuri.com/api/v1/payments/sslcommerz/ipn");

        await gateway.CreateSessionAsync(Request, TestContext.Current.CancellationToken);

        Assert.Equal("https://www.ghuri.com/api/v1/payments/sslcommerz/ipn", handler.Form["ipn_url"]);
    }

    [Fact]
    public async Task LiveMode_UsesTheLiveAddress()
    {
        var (gateway, handler, _) = Create(sandbox: false);

        await gateway.CreateSessionAsync(Request, TestContext.Current.CancellationToken);

        Assert.Equal("https://securepay.sslcommerz.com/gwprocess/v4/api.php", handler.Url);
    }

    [Fact]
    public async Task Success_ReturnsThePaymentPage_AndTheSession()
    {
        var (gateway, _, _) = Create();

        var result = await gateway.CreateSessionAsync(Request, TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal("https://sandbox.sslcommerz.com/EasyCheckOut/testcdeF2A7C1E3B9D4", result.PaymentPageUrl);
        Assert.Equal("F2A7C1E3B9D4", result.SessionId);
    }

    [Fact]
    public async Task Failed_ReturnsSslCommerzsReason()
    {
        var (gateway, _, _) = Create(Json("""{"status":"FAILED","failedreason":"Store Credential Error Or Store is De-active"}"""));

        var result = await gateway.CreateSessionAsync(Request, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal("Store Credential Error Or Store is De-active", result.FailureReason);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "{}")]
    [InlineData(HttpStatusCode.OK, "<html>Maintenance</html>")] // not JSON at all
    [InlineData(HttpStatusCode.OK, """{"status":"SUCCESS"}""")] // "success" without a page to send the customer to
    public async Task AnAnswerWeCantUse_IsAFailure_NotACrash(HttpStatusCode status, string body)
    {
        var (gateway, _, _) = Create(Json(body, status));

        var result = await gateway.CreateSessionAsync(Request, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
    }

    [Fact]
    public async Task Unreachable_IsAFailure_NotACrash()
    {
        var (gateway, _, _) = Create(throws: new HttpRequestException("No such host is known."));

        var result = await gateway.CreateSessionAsync(Request, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Contains("could not be reached", result.FailureReason);
    }

    [Fact]
    public async Task TheStorePassword_NeverReachesTheLogs()
    {
        foreach (var answer in new[] { Json(SuccessJson), Json("""{"status":"FAILED","failedreason":"x"}"""), Json("oops") })
        {
            var (gateway, _, logger) = Create(answer);

            await gateway.CreateSessionAsync(Request, TestContext.Current.CancellationToken);

            Assert.NotEmpty(logger.Lines);
            Assert.DoesNotContain(logger.Lines, line => line.Contains(StorePassword, StringComparison.Ordinal));
        }
    }

    /// <summary>Records the request instead of sending it, and answers with what the test chose.</summary>
    // ---------- Validation API (Day 10) ----------

    private const string ValidJson =
        """{"status":"VALID","tran_id":"PAY100001","val_id":"2410021530abc","amount":"34000.00","store_amount":"33490.00","currency":"BDT","bank_tran_id":"2410021530XYZ","card_type":"BKASH-BKash","currency_type":"BDT","currency_amount":"34000.00","risk_level":"0","risk_title":"Safe"}""";

    [Fact]
    public async Task Validation_AsksSslCommerzsValidator_ForThatValId()
    {
        var (gateway, handler, _) = Create(Json(ValidJson));

        await gateway.ValidatePaymentAsync("2410021530abc", TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.StartsWith("https://sandbox.sslcommerz.com/validator/api/validationserverAPI.php?val_id=2410021530abc&store_id=ghuritest123&", handler.Url);
        Assert.EndsWith("&v=1&format=json", handler.Url);
    }

    [Fact]
    public async Task Validation_Valid_ReadsTheTransactionAmountAndHowItWasPaid()
    {
        var (gateway, _, logger) = Create(Json(ValidJson));

        var result = await gateway.ValidatePaymentAsync("2410021530abc", TestContext.Current.CancellationToken);

        Assert.Equal(PaymentValidationOutcome.Valid, result.Outcome);
        Assert.Equal(("PAY100001", (decimal?)34_000m, "BDT"), (result.TransactionId, result.Amount, result.Currency));
        Assert.Equal(("BKASH-BKash", "2410021530XYZ", (decimal?)510m, false),
            (result.Method, result.ProviderTransactionId, result.GatewayFee, result.IsHighRisk));
        Assert.DoesNotContain(logger.Lines, line => line.Contains(StorePassword));
    }

    [Fact]
    public async Task Validation_AlreadyValidated_NumbersNotText_AndHighRisk_AreAllUnderstood()
    {
        var (gateway, _, _) = Create(Json(
            """{"status":"VALIDATED","tran_id":"PAY100001","amount":34000.00,"store_amount":33490,"currency":"BDT","risk_level":"1","risk_title":"Risky"}"""));

        var result = await gateway.ValidatePaymentAsync("v", TestContext.Current.CancellationToken);

        Assert.Equal((PaymentValidationOutcome.Valid, (decimal?)34_000m, true), (result.Outcome, result.Amount, result.IsHighRisk));
    }

    [Fact]
    public async Task Validation_InvalidTransaction_IsInvalid()
    {
        var (gateway, _, _) = Create(Json("""{"status":"INVALID_TRANSACTION"}"""));

        var result = await gateway.ValidatePaymentAsync("fake", TestContext.Current.CancellationToken);

        Assert.Equal(PaymentValidationOutcome.Invalid, result.Outcome);
        Assert.Contains("INVALID_TRANSACTION", result.FailureReason);
    }

    [Fact]
    public async Task Validation_NoAnswer_IsUnreachable_AndThePasswordIsNeverLogged()
    {
        var (gateway, _, logger) = Create(throws: new HttpRequestException("Connection refused"));

        var result = await gateway.ValidatePaymentAsync("v", TestContext.Current.CancellationToken);

        Assert.Equal(PaymentValidationOutcome.Unreachable, result.Outcome);
        Assert.DoesNotContain(logger.Lines, line => line.Contains(StorePassword));
    }

    [Fact]
    public async Task Validation_AServerError_IsUnreachable_SoItIsAskedAgainLater() =>
        Assert.Equal(PaymentValidationOutcome.Unreachable,
            (await Create(Json("oops", HttpStatusCode.BadGateway)).Gateway.ValidatePaymentAsync("v", TestContext.Current.CancellationToken)).Outcome);

    // ---------- Refund API ----------

    private static readonly RefundStartRequest Refund = new(
        RefundId: "RF1001", ProviderTransactionId: "2410021530XYZ", Amount: 17_000m,
        Reason: "Cancelled by the customer.", Reference: "TB100001");

    private const string RefundStartedJson =
        """{"APIConnect":"DONE","bank_tran_id":"2410021530XYZ","trans_id":"PAY100001","refund_ref_id":"59bd63fea5455","status":"success","errorReason":""}""";

    [Fact]
    public async Task Refund_SendsWhatSslCommerzExpects()
    {
        var (gateway, handler, _) = Create(Json(RefundStartedJson));

        await gateway.StartRefundAsync(Refund, TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.StartsWith("https://sandbox.sslcommerz.com/validator/api/merchantTransIDvalidationAPI.php?", handler.Url);
        var query = handler.Query;
        Assert.Equal(("2410021530XYZ", "RF1001", "17000.00"), (query["bank_tran_id"], query["refund_trans_id"], query["refund_amount"]));
        Assert.Equal(("Cancelled by the customer.", "TB100001"), (query["refund_remarks"], query["refe_id"]));
        Assert.Equal(("ghuritest123", StorePassword, "json"), (query["store_id"], query["store_passwd"], query["format"]));
    }

    [Fact]
    public async Task Refund_Accepted_ReturnsSslCommerzsRefundId_AndNeverLogsThePassword()
    {
        var (gateway, _, logger) = Create(Json(RefundStartedJson));

        var result = await gateway.StartRefundAsync(Refund, TestContext.Current.CancellationToken);

        Assert.Equal((RefundStartOutcome.Started, "59bd63fea5455"), (result.Outcome, result.ProviderRefundId));
        Assert.DoesNotContain(logger.Lines, line => line.Contains(StorePassword, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Refund_AlreadyStartedEarlier_WithItsId_CountsAsStarted()
    {
        // e.g. SSLCommerz said "success" but our database couldn't save it - the retry must not look like a new refund.
        var (gateway, _, _) = Create(Json("""{"APIConnect":"DONE","refund_ref_id":"59bd63fea5455","status":"processing"}"""));

        var result = await gateway.StartRefundAsync(Refund, TestContext.Current.CancellationToken);

        Assert.Equal((RefundStartOutcome.Started, "59bd63fea5455"), (result.Outcome, result.ProviderRefundId));
    }

    [Fact]
    public async Task Refund_AlreadyStartedEarlier_WithoutAnId_IsUnconfirmed_NeverRefused()
    {
        // Refused would put it back on the list, and staff might send it by hand while SSLCommerz sends it too.
        var (gateway, _, _) = Create(Json("""{"APIConnect":"DONE","refund_ref_id":"","status":"processing"}"""));

        var result = await gateway.StartRefundAsync(Refund, TestContext.Current.CancellationToken);

        Assert.Equal(RefundStartOutcome.Unconfirmed, result.Outcome);
        Assert.Contains("merchant panel", result.FailureReason);
    }

    [Theory]
    [InlineData("""{"APIConnect":"DONE","status":"failed","errorReason":"Invalid bank tran id"}""", "SSLCommerz refused the refund: Invalid bank tran id.")]
    [InlineData("""{"APIConnect":"FAILED","status":"INVALID"}""", "SSLCommerz refused the request (FAILED).")] // wrong store password
    [InlineData("""{"APIConnect":"INACTIVE"}""", "SSLCommerz refused the request (INACTIVE).")]
    public async Task Refund_Refused_SaysWhy(string json, string reason)
    {
        var (gateway, _, _) = Create(Json(json));

        var result = await gateway.StartRefundAsync(Refund, TestContext.Current.CancellationToken);

        Assert.Equal((RefundStartOutcome.Refused, reason), (result.Outcome, result.FailureReason));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway, "{}")]
    [InlineData(HttpStatusCode.OK, "<html>Maintenance</html>")]
    [InlineData(HttpStatusCode.OK, """{"APIConnect":"DONE","status":"something new"}""")]
    public async Task Refund_AnAnswerWeCantUse_IsUnconfirmed_NotRefused(HttpStatusCode status, string body)
    {
        var (gateway, _, _) = Create(Json(body, status));

        var result = await gateway.StartRefundAsync(Refund, TestContext.Current.CancellationToken);

        Assert.Equal(RefundStartOutcome.Unconfirmed, result.Outcome);
    }

    [Fact]
    public async Task Refund_NoAnswer_IsUnconfirmed_AndThePasswordIsNeverLogged()
    {
        var (gateway, _, logger) = Create(throws: new HttpRequestException("Connection refused"));

        var result = await gateway.StartRefundAsync(Refund, TestContext.Current.CancellationToken);

        Assert.Equal(RefundStartOutcome.Unconfirmed, result.Outcome);
        Assert.DoesNotContain(logger.Lines, line => line.Contains(StorePassword, StringComparison.Ordinal));
    }

    [Fact]
    public async Task RefundStatus_AsksAboutThatRefundId()
    {
        var (gateway, handler, _) = Create(Json("""{"APIConnect":"DONE","status":"processing","refund_ref_id":"59bd63fea5455"}"""));

        await gateway.GetRefundStatusAsync("59bd63fea5455", TestContext.Current.CancellationToken);

        Assert.StartsWith("https://sandbox.sslcommerz.com/validator/api/merchantTransIDvalidationAPI.php?", handler.Url);
        Assert.Equal(("59bd63fea5455", "ghuritest123"), (handler.Query["refund_ref_id"], handler.Query["store_id"]));
        Assert.False(handler.Query.ContainsKey("refund_amount")); // only asking - never a second refund
    }

    [Theory]
    [InlineData("""{"APIConnect":"DONE","status":"refunded","refunded_on":"2026-10-09 14:02:11"}""", RefundStatusOutcome.Refunded)]
    [InlineData("""{"APIConnect":"DONE","status":"processing"}""", RefundStatusOutcome.Processing)]
    [InlineData("""{"APIConnect":"DONE","status":"cancelled"}""", RefundStatusOutcome.Failed)]
    // What the sandbox really answers for a refund id it doesn't know (checked 2026-10-09):
    [InlineData("""{"APIConnect":"DONE","bank_tran_id":"","tran_id":"","initiated_on":"0000-00-00 00:00:00","refunded_on":"0000-00-00 00:00:00","status":"failed","refund_ref_id":"x","errorReason":"Unknown Refund Ref ID"}""", RefundStatusOutcome.Failed)]
    // ...and for a wrong store password - nothing is known about the refund:
    [InlineData("""{"APIConnect":"FAILED","bank_tran_id":"","tran_id":"","status":"INVALID","refund_ref_id":"x"}""", RefundStatusOutcome.Unconfirmed)]
    [InlineData("""{"APIConnect":"DONE","status":"something new"}""", RefundStatusOutcome.Unconfirmed)]
    public async Task RefundStatus_UnderstandsEachAnswer(string json, RefundStatusOutcome expected)
    {
        var (gateway, _, _) = Create(Json(json));

        var result = await gateway.GetRefundStatusAsync("x", TestContext.Current.CancellationToken);

        Assert.Equal(expected, result.Outcome);
    }

    [Fact]
    public async Task RefundStatus_Failed_SaysWhy() =>
        Assert.Equal("SSLCommerz failed the refund: Unknown Refund Ref ID.",
            (await Create(Json("""{"APIConnect":"DONE","status":"failed","errorReason":"Unknown Refund Ref ID"}""")).Gateway
                .GetRefundStatusAsync("x", TestContext.Current.CancellationToken)).FailureReason);

    [Fact]
    public async Task RefundStatus_NoAnswer_IsUnconfirmed_AndThePasswordIsNeverLogged()
    {
        var (gateway, _, logger) = Create(throws: new TaskCanceledException("timed out"));

        var result = await gateway.GetRefundStatusAsync("x", TestContext.Current.CancellationToken);

        Assert.Equal(RefundStatusOutcome.Unconfirmed, result.Outcome);
        Assert.DoesNotContain(logger.Lines, line => line.Contains(StorePassword, StringComparison.Ordinal));
    }

    private sealed class FakeHandler(HttpResponseMessage answer, Exception? throws) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string? Url { get; private set; }
        public Dictionary<string, string> Form { get; private set; } = [];

        /// <summary>The query string of a GET (the validation and refund APIs), decoded.</summary>
        public Dictionary<string, string> Query { get; private set; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Url = request.RequestUri?.ToString();
            if (request.RequestUri is { Query.Length: > 1 } uri)
                Query = Decode(uri.Query[1..]); // still escaped, unlike ToString()
            if (request.Content is not null) // a GET (the validation API) has no body
            {
                var body = await request.Content.ReadAsStringAsync(cancellationToken);
                Form = Decode(body);
            }

            if (throws is not null)
                throw throws;
            return answer;
        }

        /// <summary>"a=1&amp;b=x%40y" → { a: "1", b: "x@y" } - a form body or a query string.</summary>
        private static Dictionary<string, string> Decode(string text) =>
            text.Split('&').Select(pair => pair.Split('=', 2))
                .ToDictionary(kv => Uri.UnescapeDataString(kv[0]), kv => Uri.UnescapeDataString(kv[1].Replace('+', ' ')));
    }

    /// <summary>Keeps every log line (message + exception) so a test can search them.</summary>
    private sealed class ListLogger : ILogger<SslCommerzGateway>
    {
        public List<string> Lines { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Lines.Add(formatter(state, exception) + " " + exception);
    }
}
