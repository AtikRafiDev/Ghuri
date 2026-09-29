using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Application.Features.Identity.Commands.RefreshSession;

namespace Ghuri.Application.Tests.Features.Identity;

public class RefreshSessionHandlerTests
{
    private readonly IdentityTestContext _context = new();

    private async Task<Result<AuthTokens>> RefreshAsync(string refreshToken) =>
        await new RefreshSessionHandler(_context.RefreshTokens, _context.Users, _context.Tokens, _context.Sessions, _context.Clock)
            .Handle(new RefreshSessionCommand(refreshToken), TestContext.Current.CancellationToken);

    /// <summary>A logged-in customer; returns the refresh token their browser would hold.</summary>
    private string LoggedInCustomer()
    {
        var user = _context.AddCustomer();
        return _context.Sessions.Issue(user, Guid.CreateVersion7(), _context.Clock.Now).RefreshToken;
    }

    [Fact]
    public async Task ValidToken_IsSwappedForANewOne_InTheSameFamily()
    {
        var first = LoggedInCustomer();

        var result = await RefreshAsync(first);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(first, result.Value.RefreshToken);
        var old = _context.RefreshTokens.Tokens[0];
        var replacement = _context.RefreshTokens.Tokens[1];
        Assert.True(old.IsRotated);                         // the old one is spent
        Assert.Equal(old.FamilyId, replacement.FamilyId);   // same login, continued
    }

    [Fact]
    public async Task SpentTokenUsedAgainLater_EndsTheWholeLogin()
    {
        // The thief's copy and the real user's copy are the same token.
        var stolen = LoggedInCustomer();
        await RefreshAsync(stolen);                           // the real user refreshes first
        _context.Clock.Now = _context.Clock.Now.AddMinutes(5);

        var thief = await RefreshAsync(stolen);               // then the thief tries the spent copy

        Assert.Equal("session_expired", thief.Error.Code);
        Assert.True(thief.Error.CommitChanges);               // the revoke must be saved
        Assert.All(_context.RefreshTokens.Tokens, t => Assert.False(t.IsActive(_context.Clock.Now)));
    }

    [Fact]
    public async Task SpentTokenUsedAgainWithinSeconds_IsRefused_ButDoesNotLogEveryoneOut()
    {
        // Two tabs opened together both sent the same cookie.
        var shared = LoggedInCustomer();
        var firstTab = await RefreshAsync(shared);

        var secondTab = await RefreshAsync(shared);

        Assert.Equal("session_expired", secondTab.Error.Code);
        var firstTabsToken = _context.RefreshTokens.Tokens.Single(t => t.TokenHash == $"HASH({firstTab.Value.RefreshToken})");
        Assert.True(firstTabsToken.IsActive(_context.Clock.Now)); // the real session survives
    }

    [Fact]
    public async Task TokenOlderThan30Days_IsRefused()
    {
        var token = LoggedInCustomer();
        _context.Clock.Now = _context.Clock.Now.AddDays(30);

        var result = await RefreshAsync(token);

        Assert.Equal("session_expired", result.Error.Code);
    }

    [Fact]
    public async Task UnknownToken_IsRefused()
    {
        var result = await RefreshAsync("made-up-token");

        Assert.Equal("session_expired", result.Error.Code);
    }
}
