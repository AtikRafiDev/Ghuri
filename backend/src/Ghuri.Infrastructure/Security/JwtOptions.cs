namespace Ghuri.Infrastructure.Security;

/// <summary>
/// Settings for the access tokens, read from the "Jwt" section of
/// configuration (blueprint section 9: Options pattern, "typed
/// configuration validated at startup").
/// </summary>
/// <remarks>
/// Issuer, Audience and AccessTokenMinutes live in appsettings.json.
/// SigningKey lives in appsettings.Development.json and IS committed to
/// git - acceptable only because this is a local learning project that
/// never runs anywhere else. A real deployment must move it back to
/// "dotnet user-secrets" locally / the Jwt__SigningKey environment
/// variable on a server, since anyone with this value can forge a valid
/// token for any user.
/// Public because the Api's JWT validation (Day 2, step 4) must check
/// tokens with exactly these values.
/// </remarks>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>The claim type carrying role names - the Api's token validation must use the same one.</summary>
    public const string RoleClaimType = "role";

    /// <summary>Who created the token (this API).</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Who the token is for (our frontend).</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>Secret used to sign tokens. Anyone who has it can create valid tokens for any user.</summary>
    public string SigningKey { get; init; } = string.Empty;

    public int AccessTokenMinutes { get; init; } = 15;
}
