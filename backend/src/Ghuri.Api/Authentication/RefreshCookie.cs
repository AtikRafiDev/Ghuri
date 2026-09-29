namespace Ghuri.Api.Authentication;

/// <summary>
/// The "stay logged in" cookie that carries the refresh token (blueprint
/// section 8: "HttpOnly; Secure; SameSite=Strict cookie").
/// </summary>
/// <remarks>
/// Every setting protects against a specific attack:
/// <list type="bullet">
/// <item>HttpOnly - JavaScript can't read it, so a malicious script injected
/// into the page (XSS) can't steal it.</item>
/// <item>Secure - only sent over HTTPS (browsers treat http://localhost as
/// safe too, so local development still works).</item>
/// <item>SameSite=Strict - never sent when ANOTHER site triggers the request,
/// so evil.com can't make your browser refresh your session (CSRF).</item>
/// <item>Path=/api/v1/auth - only sent to the auth endpoints (refresh,
/// logout), not attached to every single API call.</item>
/// </list>
/// </remarks>
internal static class RefreshCookie
{
    public const string Name = "ghuri_refresh";
    private const string AuthPath = "/api/v1/auth";

    public static void Append(HttpResponse response, string refreshToken, DateTime expiresAtUtc) =>
        response.Cookies.Append(Name, refreshToken, Options(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)));

    /// <summary>Tells the browser to throw the cookie away. Must use the same Path, or the browser keeps it.</summary>
    public static void Delete(HttpResponse response) => response.Cookies.Delete(Name, Options(expires: null));

    private static CookieOptions Options(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = AuthPath,
        Expires = expires,
        IsEssential = true, // needed for login to work at all - not a tracking cookie
    };
}
