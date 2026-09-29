namespace Ghuri.Api.Controllers.Auth;

/// <summary>
/// The JSON body after login/register/refresh/change-password. Holds ONLY
/// the short-lived access token - the refresh token travels separately, in
/// the HttpOnly cookie, where JavaScript can't reach it.
/// </summary>
/// <param name="AccessToken">Send as "Authorization: Bearer ..." on every API call. Keep it in memory, never in localStorage.</param>
/// <param name="ExpiresAtUtc">When to refresh (the frontend can also just wait for a 401).</param>
public sealed record AccessTokenResponse(string AccessToken, DateTime ExpiresAtUtc);
