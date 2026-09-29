using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ghuri.Infrastructure.Security;

/// <summary>ITokenService: signed JWT access tokens + random opaque tokens.</summary>
internal sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    public AccessToken CreateAccessToken(User user)
    {
        var settings = options.Value;
        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var expiresAtUtc = nowUtc.AddMinutes(settings.AccessTokenMinutes);

        // Claims = the facts written inside the token. Kept small: the
        // token travels with EVERY request, and anyone can READ it (it is
        // signed, not encrypted) - so never put secrets in here.
        var claims = new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // who - read back by HttpCurrentUser
            new Claim(JwtRegisteredClaimNames.Name, user.FullName),
        ]);
        if (user.Email is not null)
            claims.AddClaim(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        foreach (var role in user.Roles)
            claims.AddClaim(new Claim(JwtOptions.RoleClaimType, ((SystemRole)role.RoleId).ToString()));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Subject = claims,
            IssuedAt = nowUtc,
            NotBefore = nowUtc,
            Expires = expiresAtUtc,
            // HMAC-SHA256: the API both signs and checks tokens with the
            // same secret key - the simplest safe choice when one server
            // is the only one creating and reading them.
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(TokenHandler.CreateToken(descriptor), expiresAtUtc);
    }

    public OpaqueToken CreateOpaqueToken()
    {
        // 64 random bytes from the OS's cryptographic generator (blueprint
        // section 8: "random 64 bytes") - impossible to guess. Base64Url
        // makes it safe inside a URL, for the reset-password link.
        var value = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));
        return new OpaqueToken(value, HashOpaqueToken(value));
    }

    // A plain, fast SHA-256 is right HERE (unlike passwords): the token is
    // already 512 random bits, so there is nothing to brute-force - and an
    // unsalted hash can be looked up directly ("WHERE TokenHash = @hash").
    public string HashOpaqueToken(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
