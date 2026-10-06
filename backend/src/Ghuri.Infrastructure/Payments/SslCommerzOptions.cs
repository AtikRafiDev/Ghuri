namespace Ghuri.Infrastructure.Payments;

/// <summary>
/// Settings from "PaymentGateway:SslCommerz". The Store ID and password are
/// in appsettings.Development.json for the SANDBOX; live keys will be server
/// environment variables (PaymentGateway__SslCommerz__StorePassword), never in git.
/// </summary>
internal sealed class SslCommerzOptions
{
    public const string SectionName = "PaymentGateway:SslCommerz";

    public string StoreId { get; init; } = string.Empty;
    public string StorePassword { get; init; } = string.Empty;

    /// <summary>true = sandbox.sslcommerz.com (test cards, no real money); false = securepay.sslcommerz.com (live).</summary>
    public bool UseSandbox { get; init; } = true;

    public string SandboxBaseUrl { get; init; } = "https://sandbox.sslcommerz.com";
    public string LiveBaseUrl { get; init; } = "https://securepay.sslcommerz.com";

    /// <summary>
    /// Where the customer's BROWSER comes back to after paying: our site's
    /// address. Locally http://localhost:5173 - Vite forwards /api to the API -
    /// or the tunnel's https address (README "Public address", set in user-secrets).
    /// The paths (/api/v1/payments/sslcommerz/success…) are added by the gateway.
    /// </summary>
    public string CallbackBaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Where SSLCommerz's SERVER notifies ours (IPN). Must be reachable from
    /// the internet: locally only through the tunnel (README "Public address").
    /// Empty = not sent at all.
    /// </summary>
    public string? IpnUrl { get; init; }

    // SSLCommerz requires an address; we sell trips, nothing is shipped, so
    // the agency's own city is sent instead of asking every customer for one.
    public string CustomerAddress { get; init; } = "Dhaka";
    public string CustomerCity { get; init; } = "Dhaka";
    public string CustomerPostcode { get; init; } = "1000";
    public string CustomerCountry { get; init; } = "Bangladesh";

    public string ProductCategory { get; init; } = "Travel";

    /// <summary>A trip is a service: "non-physical-goods" needs no extra fields ("travel-vertical" would want a hotel name per booking).</summary>
    public string ProductProfile { get; init; } = "non-physical-goods";

    public int TimeoutSeconds { get; init; } = 30;

    public string BaseUrl => UseSandbox ? SandboxBaseUrl : LiveBaseUrl;
}
