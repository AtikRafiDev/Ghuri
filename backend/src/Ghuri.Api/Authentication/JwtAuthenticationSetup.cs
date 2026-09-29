using System.Text;
using Ghuri.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ghuri.Api.Authentication;

/// <summary>
/// How the API CHECKS an access token on every request - the mirror image
/// of JwtTokenService, which CREATES them. Both read the same JwtOptions,
/// so they can never disagree on key, issuer or audience.
/// </summary>
internal static class JwtAuthenticationSetup
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured through DI (not by reading appsettings here), so it
        // uses the SAME validated JwtOptions object the token service uses.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Keep claim names exactly as written in the token ("sub",
                // "role") instead of ASP.NET Core's old habit of renaming
                // them to long URLs like ".../nameidentifier".
                bearer.MapInboundClaims = false;

                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    // 1. Signature: recompute it with OUR key. Any change to
                    //    the token - even one role name - breaks it.
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    // Only the algorithm we sign with - refuses tricks like
                    // a token claiming "alg": "none".
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],

                    // 2. Made by us, for our frontend.
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,

                    // 3. Not expired. The default tolerance is 5 MINUTES,
                    //    which would quietly turn "15 minutes" into 20. 30
                    //    seconds still covers small clock differences.
                    ClockSkew = TimeSpan.FromSeconds(30),

                    // Tell ASP.NET Core which claims hold the name and the
                    // roles - otherwise [Authorize(Roles=...)] and our
                    // policies would never find the "role" claims.
                    NameClaimType = JwtRegisteredClaimNames.Name,
                    RoleClaimType = JwtOptions.RoleClaimType,
                };
            });

        return services;
    }
}
