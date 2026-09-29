using Ghuri.Api.Authentication;
using Ghuri.Api.ErrorHandling;
using Ghuri.Api.RateLimiting;
using Ghuri.Application.Features.Identity;
using Ghuri.Application.Features.Identity.Commands.ChangePassword;
using Ghuri.Application.Features.Identity.Commands.ForgotPassword;
using Ghuri.Application.Features.Identity.Commands.Login;
using Ghuri.Application.Features.Identity.Commands.Logout;
using Ghuri.Application.Features.Identity.Commands.RefreshSession;
using Ghuri.Application.Features.Identity.Commands.RegisterCustomer;
using Ghuri.Application.Features.Identity.Commands.ResetPassword;
using Ghuri.Application.Features.Identity.Queries.GetMe;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ghuri.Api.Controllers.Auth;

/// <summary>
/// Accounts and sessions (blueprint section 11: "Auth (public)").
/// </summary>
/// <remarks>
/// A THIN controller (blueprint section 4.4): it only translates HTTP into
/// commands and Results back into HTTP - plus the one thing only HTTP
/// knows about, the refresh cookie. Every rule lives in the handlers.
/// </remarks>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    /// <summary>Create a customer account; logs them straight in.</summary>
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> Register(RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? SignedIn(result.Value, created: true) : ResultExtensions.ToProblem(result.Error);
    }

    /// <summary>Log in with phone or email + password.</summary>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? SignedIn(result.Value) : ResultExtensions.ToProblem(result.Error);
    }

    /// <summary>New access token from the refresh cookie (sent automatically by the browser).</summary>
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicies.AuthRefresh)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        // No cookie = never logged in on this browser. Every visitor's
        // first page load asks, so answer without touching the database.
        // "not_authenticated", NOT "session_expired": the frontend retries
        // a session_expired once (another tab may have just replaced the
        // cookie) - pointless when there was no cookie at all.
        var refreshToken = Request.Cookies[RefreshCookie.Name];
        if (string.IsNullOrEmpty(refreshToken))
            return ResultExtensions.ToProblem(IdentityErrors.NotAuthenticated);

        var result = await sender.Send(new RefreshSessionCommand(refreshToken), cancellationToken);
        if (result.IsSuccess)
            return SignedIn(result.Value);

        RefreshCookie.Delete(Response); // a dead cookie is useless - throw it away
        return ResultExtensions.ToProblem(result.Error);
    }

    /// <summary>End this device's session. Always 204, even if already logged out.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogoutCommand(Request.Cookies[RefreshCookie.Name]), cancellationToken);
        RefreshCookie.Delete(Response);
        return result.ToActionResult();
    }

    /// <summary>Email a reset link. Always 204, so it never reveals whether the email is registered.</summary>
    [HttpPost("password/forgot")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();

    /// <summary>Set a new password with the email + token from the reset link. Logs out every device.</summary>
    [HttpPost("password/reset")]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command, cancellationToken)).ToActionResult();

    /// <summary>Change password while logged in. Other devices are logged out; this one gets a fresh session.</summary>
    [HttpPost("password/change")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.AuthStrict)]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? SignedIn(result.Value) : ResultExtensions.ToProblem(result.Error);
    }

    /// <summary>The logged-in user's profile and roles.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken) =>
        (await sender.Send(new GetMeQuery(), cancellationToken)).ToActionResult();

    /// <summary>
    /// Splits a new session in two: the refresh token into the HttpOnly
    /// cookie, the access token into the JSON body.
    /// </summary>
    private IActionResult SignedIn(AuthTokens tokens, bool created = false)
    {
        RefreshCookie.Append(Response, tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc);
        var body = new AccessTokenResponse(tokens.AccessToken, tokens.AccessTokenExpiresAtUtc);

        // 201 for a new account, pointing at where to read it back.
        return created ? Created("/api/v1/auth/me", body) : Ok(body);
    }
}
