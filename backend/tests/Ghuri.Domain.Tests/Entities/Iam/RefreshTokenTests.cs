using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Exceptions;

namespace Ghuri.Domain.Tests.Entities.Iam;

public class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);

    private static readonly string OldHash = new('a', 64);
    private static readonly string NewHash = new('b', 64);

    // A fresh 30-day token.
    private static RefreshToken NewToken() =>
        RefreshToken.Issue(Guid.NewGuid(), OldHash, Guid.NewGuid(), Now, Now.AddDays(30), createdByIp: null, userAgent: null);

    [Fact]
    public void IsActive_UntilTheDeadline()
    {
        var token = NewToken();

        Assert.True(token.IsActive(Now.AddDays(29)));
        Assert.False(token.IsActive(Now.AddDays(30)));
    }

    [Fact]
    public void ReplaceWith_SpendsTheToken_AndRemembersItsReplacement()
    {
        var token = NewToken();

        token.ReplaceWith(NewHash, Now.AddMinutes(15));

        Assert.False(token.IsActive(Now.AddMinutes(15)));
        Assert.True(token.IsRotated);
        Assert.Equal(NewHash, token.ReplacedByHash);
    }

    [Fact]
    public void ReplaceWith_OnAnAlreadyRotatedToken_IsRefused()
    {
        // The handler must treat this case as REUSE (revoke the family);
        // the entity makes sure it can never be silently rotated twice.
        var token = NewToken();
        token.ReplaceWith(NewHash, Now);

        var error = Assert.Throws<DomainException>(() => token.ReplaceWith(new string('c', 64), Now));

        Assert.Equal("refresh_token_inactive", error.Code);
    }

    [Fact]
    public void Revoke_ByLogout_IsNotMistakenForReuse()
    {
        var token = NewToken();

        token.Revoke(Now);

        Assert.False(token.IsActive(Now));
        Assert.False(token.IsRotated);
    }

    [Fact]
    public void Revoke_Twice_KeepsTheFirstTime()
    {
        var token = NewToken();

        token.Revoke(Now);
        token.Revoke(Now.AddHours(1));

        Assert.Equal(Now, token.RevokedAtUtc);
    }
}
