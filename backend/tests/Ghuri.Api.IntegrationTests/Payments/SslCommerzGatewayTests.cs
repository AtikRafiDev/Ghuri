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
    private sealed class FakeHandler(HttpResponseMessage answer, Exception? throws) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string? Url { get; private set; }
        public Dictionary<string, string> Form { get; private set; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Url = request.RequestUri?.ToString();
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            Form = body.Split('&').Select(pair => pair.Split('=', 2))
                .ToDictionary(kv => Uri.UnescapeDataString(kv[0]), kv => Uri.UnescapeDataString(kv[1].Replace('+', ' ')));

            if (throws is not null)
                throw throws;
            return answer;
        }
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
