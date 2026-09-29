using Ghuri.Domain.Entities.Iam;

namespace Ghuri.Application.Abstractions.Ports;

/// <summary>
/// Creates the two kinds of token the app hands out (blueprint section 9.3):
/// <list type="bullet">
/// <item><b>Access token</b> - a signed JWT the browser sends with every
/// API call. Short-lived (15 min), never stored in the database.</item>
/// <item><b>Opaque token</b> - just a long random string with no meaning
/// inside it. Used for refresh tokens and password-reset links. Only its
/// hash is stored, so a leaked database can't be used to log in.</item>
/// </list>
/// </summary>
public interface ITokenService
{
    /// <summary>A signed JWT carrying the user's id, name, email and roles.</summary>
    /// <remarks>Load the user WITH their roles first (IUserRepository does), or the token gets no roles.</remarks>
    AccessToken CreateAccessToken(User user);

    /// <summary>A new random token: Value goes to the user (cookie / email link), Hash goes to the database.</summary>
    OpaqueToken CreateOpaqueToken();

    /// <summary>The hash of a token the user sent back - used to look it up in the database.</summary>
    string HashOpaqueToken(string value);
}

/// <param name="Value">The JWT string for the Authorization header.</param>
/// <param name="ExpiresAtUtc">When it stops working - the frontend can refresh just before this.</param>
public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

/// <param name="Value">The raw token. Given to the user ONCE, never stored or logged.</param>
/// <param name="Hash">SHA-256 of Value, 64 hex characters - fits iam.RefreshTokens.TokenHash / iam.OtpCodes.CodeHash.</param>
public sealed record OpaqueToken(string Value, string Hash);
