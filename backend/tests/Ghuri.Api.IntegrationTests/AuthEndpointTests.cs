using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Ghuri.Api.IntegrationTests;

/// <summary>
/// The real HTTP pipeline - JWT check, [Authorize], validation, ProblemDetails.
/// Every test here stops BEFORE a handler would need SQL Server, so they
/// run on the CI server, which has no database.
/// </summary>
public class AuthEndpointTests(GhuriApiFactory factory) : IClassFixture<GhuriApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Me_WithoutAToken_Is401()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/auth/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithATokenSignedByAnotherKey_Is401_EvenClaimingSuperAdmin()
    {
        // A forger's token: right issuer, audience, even a SuperAdmin role -
        // but signed with a key that isn't ours. The signature check fails.
        var forged = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "ghuri-api",
            Audience = "ghuri-web",
            Subject = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), new Claim("role", "SuperAdmin")]),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-completely-different-key-of-the-forger!")),
                SecurityAlgorithms.HmacSha256),
        });
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        var response = await client.GetAsync("/api/v1/auth/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ARealToken_GetsPastTheJwtCheck()
    {
        // A token made by the app's own token service (same key) lets the
        // request through [Authorize] - proven by it reaching the validator
        // (400 for a too-short new password) instead of a 401.
        var user = User.Create("Test Admin", PhoneNumber.Create("01700000001"), null, null);
        user.AssignRole(SystemRole.SuperAdmin, DateTime.UtcNow);
        var accessToken = factory.Services.GetRequiredService<ITokenService>().CreateAccessToken(user);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Value);

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/password/change", new { currentPassword = "anything", newPassword = "short" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("validation_failed", await ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Login_WithBadInput_Is400_WithFieldErrors()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login", new { phoneOrEmail = "01711000000", password = new string('x', 129) }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("validation_failed", body.GetProperty("code").GetString());
        Assert.True(body.GetProperty("errors").TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task Refresh_WithoutACookie_Is401_NotAuthenticated()
    {
        // Not "session_expired" - there never was a session in this browser,
        // so the frontend shouldn't bother retrying.
        var response = await factory.CreateClient().PostAsync("/api/v1/auth/refresh", content: null, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("not_authenticated", await ErrorCodeAsync(response));
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("code").GetString();
}
