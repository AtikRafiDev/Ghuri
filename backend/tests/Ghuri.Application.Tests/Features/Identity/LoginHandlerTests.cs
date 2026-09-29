using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Application.Features.Identity.Commands.Login;

namespace Ghuri.Application.Tests.Features.Identity;

public class LoginHandlerTests
{
    private readonly IdentityTestContext _context = new();

    private async Task<Result<AuthTokens>> LoginAsync(string phoneOrEmail, string password) =>
        await new LoginHandler(_context.Users, _context.Passwords, _context.Sessions, _context.Settings, _context.Clock)
            .Handle(new LoginCommand(phoneOrEmail, password), TestContext.Current.CancellationToken);

    [Fact]
    public async Task RightPassword_StartsASession_StoringOnlyTheRefreshTokensHash()
    {
        _context.AddCustomer();

        var result = await LoginAsync("01711000000", IdentityTestContext.Password);

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(_context.RefreshTokens.Tokens);
        Assert.Equal($"HASH({result.Value.RefreshToken})", stored.TokenHash);
        Assert.NotEqual(result.Value.RefreshToken, stored.TokenHash);
    }

    [Fact]
    public async Task Email_IsAcceptedInAnyLetterCase()
    {
        _context.AddCustomer(email: "rahim@example.com");

        var result = await LoginAsync("  RAHIM@Example.com ", IdentityTestContext.Password);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task WrongPassword_IsCounted_AndTheCountIsKept()
    {
        var user = _context.AddCustomer();

        var result = await LoginAsync("01711000000", "wrong");

        Assert.Equal("invalid_credentials", result.Error.Code);
        Assert.True(result.Error.CommitChanges); // otherwise the +1 would be rolled back
        Assert.Equal(1, user.AccessFailedCount);
    }

    [Fact]
    public async Task UnknownUser_GetsTheSameAnswerAsAWrongPassword()
    {
        var result = await LoginAsync("01899999999", "anything");

        Assert.Equal("invalid_credentials", result.Error.Code);
    }

    [Fact]
    public async Task FifthWrongPassword_LocksTheAccount_AndTheLockIsKept()
    {
        var user = _context.AddCustomer();
        for (var i = 0; i < 4; i++)
            await LoginAsync("01711000000", "wrong");

        var fifth = await LoginAsync("01711000000", "wrong");

        Assert.Equal("account_locked", fifth.Error.Code);
        Assert.True(fifth.Error.CommitChanges);
        Assert.True(user.IsLockedOut(_context.Clock.Now));
    }

    [Fact]
    public async Task WhileLocked_EvenTheRightPasswordIsRefused_UntilTheLockEnds()
    {
        _context.AddCustomer();
        for (var i = 0; i < 5; i++)
            await LoginAsync("01711000000", "wrong");

        var duringLock = await LoginAsync("01711000000", IdentityTestContext.Password);
        _context.Clock.Now = _context.Clock.Now.AddMinutes(15);
        var afterLock = await LoginAsync("01711000000", IdentityTestContext.Password);

        Assert.Equal("account_locked", duringLock.Error.Code);
        Assert.True(afterLock.IsSuccess);
    }
}
