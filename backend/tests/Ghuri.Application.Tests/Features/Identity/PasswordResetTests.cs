using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity.Commands.ForgotPassword;
using Ghuri.Application.Features.Identity.Commands.ResetPassword;

namespace Ghuri.Application.Tests.Features.Identity;

/// <summary>The whole "Forgot password" journey: request a link, then use it.</summary>
public class PasswordResetTests
{
    private const string NewPassword = "brand-new-password";
    private readonly IdentityTestContext _context = new();

    private async Task<Result> ForgotAsync(string email) =>
        await new ForgotPasswordHandler(_context.Users, _context.Otps, _context.Tokens, _context.Emails, _context.Settings, _context.Clock)
            .Handle(new ForgotPasswordCommand(email), TestContext.Current.CancellationToken);

    private async Task<Result> ResetAsync(string email, string token) =>
        await new ResetPasswordHandler(_context.Users, _context.Otps, _context.RefreshTokens, _context.Tokens, _context.Passwords, _context.Clock)
            .Handle(new ResetPasswordCommand(email, token, NewPassword), TestContext.Current.CancellationToken);

    /// <summary>Asks for a link and returns the token that was put in the email.</summary>
    private async Task<string> RequestLinkAsync(string email = "rahim@example.com")
    {
        await ForgotAsync(email);
        return _context.Tokens.LastCreated!.Value;
    }

    [Fact]
    public async Task Forgot_KnownEmail_EmailsALinkValidFor30Minutes_StoringOnlyTheHash()
    {
        _context.AddCustomer();

        var token = await RequestLinkAsync();

        var email = Assert.Single(_context.Emails.Sent);
        Assert.Contains($"token={token}", email.HtmlBody);
        var code = Assert.Single(_context.Otps.Codes);
        Assert.Equal($"HASH({token})", code.CodeHash);
        Assert.Equal(IdentityTestContext.Start.AddMinutes(30), code.ExpiresAtUtc);
    }

    [Fact]
    public async Task Forgot_UnknownEmail_SucceedsAnyway_ButSendsNothing()
    {
        var result = await ForgotAsync("nobody@example.com");

        Assert.True(result.IsSuccess); // a stranger learns nothing
        Assert.Empty(_context.Emails.Sent);
    }

    [Fact]
    public async Task Forgot_SixthRequestWithinAnHour_IsQuietlyNotSent()
    {
        _context.AddCustomer();
        for (var i = 0; i < 6; i++)
            await ForgotAsync("rahim@example.com");

        Assert.Equal(5, _context.Emails.Sent.Count);
    }

    [Fact]
    public async Task Reset_WithTheEmailedToken_SetsThePassword_UsesUpTheLink_AndLogsOutEverywhere()
    {
        var user = _context.AddCustomer();
        _context.Sessions.Issue(user, Guid.CreateVersion7(), _context.Clock.Now); // logged in on some device
        var token = await RequestLinkAsync();

        var result = await ResetAsync("rahim@example.com", token);

        Assert.True(result.IsSuccess);
        Assert.True(_context.Passwords.Verify(user.PasswordHash!, NewPassword));
        Assert.NotNull(Assert.Single(_context.Otps.Codes).ConsumedAtUtc);
        Assert.All(_context.RefreshTokens.Tokens, t => Assert.False(t.IsActive(_context.Clock.Now)));

        var secondUse = await ResetAsync("rahim@example.com", token);
        Assert.Equal("reset_link_invalid", secondUse.Error.Code); // works once only
    }

    [Fact]
    public async Task Reset_WithAWrongToken_CountsTheAttempt_AndKeepsTheCount()
    {
        _context.AddCustomer();
        await RequestLinkAsync();

        var result = await ResetAsync("rahim@example.com", "guessed-token");

        Assert.Equal("reset_link_invalid", result.Error.Code);
        Assert.True(result.Error.CommitChanges);
        Assert.Equal(1, Assert.Single(_context.Otps.Codes).Attempts);
    }

    [Fact]
    public async Task Reset_After30Minutes_IsRefused()
    {
        _context.AddCustomer();
        var token = await RequestLinkAsync();
        _context.Clock.Now = _context.Clock.Now.AddMinutes(30);

        var result = await ResetAsync("rahim@example.com", token);

        Assert.Equal("reset_link_invalid", result.Error.Code);
    }

    [Fact]
    public async Task Reset_OnlyTheNewestLinkWorks()
    {
        _context.AddCustomer();
        var olderToken = await RequestLinkAsync();
        await RequestLinkAsync();

        var result = await ResetAsync("rahim@example.com", olderToken);

        Assert.Equal("reset_link_invalid", result.Error.Code);
    }
}
