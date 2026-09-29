namespace Ghuri.Infrastructure.Security;

/// <summary>
/// Settings for the access tokens, read from the "Jwt" section of
/// configuration (blueprint section 9: Options pattern, "typed
/// configuration validated at startup").
/// </summary>
/// <remarks>
/// Issuer, Audience and AccessTokenMinutes are not secret and live in
/// appsettings.json. SigningKey IS secret and never goes into git: locally
/// it comes from "dotnet user-secrets", on a server from the
/// Jwt__SigningKey environment variable.
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
