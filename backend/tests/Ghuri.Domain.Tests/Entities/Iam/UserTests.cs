using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Tests.Entities.Iam;

public class UserTests
{
    // The same limits appsettings.json will hold ("Auth" section).
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // A fixed "now" - tests pretend time passes with Now.AddMinutes(...)
    // instead of waiting 15 real minutes.
    private static readonly DateTime Now = new(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);

    private static User NewUser() =>
        User.Create("Rahim Uddin", PhoneNumber.Create("01711000000"), "rahim@example.com", "hash-1");

    private static void FailLogins(User user, int times, DateTime nowUtc)
    {
        for (var i = 0; i < times; i++)
            user.RecordFailedLogin(nowUtc, MaxFailedAttempts, LockoutDuration);
    }

    [Fact]
    public void RecordFailedLogin_BelowTheLimit_CountsButDoesNotLock()
    {
        var user = NewUser();

        FailLogins(user, 4, Now);

        Assert.Equal(4, user.AccessFailedCount);
        Assert.False(user.IsLockedOut(Now));
    }

    [Fact]
    public void RecordFailedLogin_FifthFailure_LocksFor15Minutes_AndRestartsTheCount()
    {
        var user = NewUser();

        FailLogins(user, 5, Now);

        Assert.Equal(Now.AddMinutes(15), user.LockoutEndUtc);
        Assert.Equal(0, user.AccessFailedCount);
        Assert.True(user.IsLockedOut(Now.AddMinutes(14)));
    }

    [Fact]
    public void IsLockedOut_EndsOnItsOwn_WhenTheDeadlinePasses()
    {
        var user = NewUser();
        FailLogins(user, 5, Now);

        // Nothing called "unlock" - time simply passed LockoutEndUtc.
        Assert.False(user.IsLockedOut(Now.AddMinutes(15)));
    }

    [Fact]
    public void RecordFailedLogin_WhileLocked_DoesNotExtendTheLock()
    {
        var user = NewUser();
        FailLogins(user, 5, Now);

        FailLogins(user, 5, Now.AddMinutes(10));

        Assert.Equal(Now.AddMinutes(15), user.LockoutEndUtc);
        Assert.Equal(0, user.AccessFailedCount);
    }

    [Fact]
    public void RecordSuccessfulLogin_ForgetsEarlierFailures()
    {
        var user = NewUser();
        FailLogins(user, 3, Now);

        user.RecordSuccessfulLogin(Now);

        Assert.Equal(0, user.AccessFailedCount);
        Assert.Equal(Now, user.LastLoginUtc);
    }

    [Fact]
    public void RecordSuccessfulLogin_WhileLocked_IsRefused_EvenWithTheRightPassword()
    {
        var user = NewUser();
        FailLogins(user, 5, Now);

        var error = Assert.Throws<DomainException>(() => user.RecordSuccessfulLogin(Now.AddMinutes(1)));

        Assert.Equal("account_locked", error.Code);
    }

    [Fact]
    public void SetPassword_ReplacesHashAndSecurityStamp_AndClearsALock()
    {
        var user = NewUser();
        var oldStamp = user.SecurityStamp;
        FailLogins(user, 5, Now);

        user.SetPassword("hash-2");

        Assert.Equal("hash-2", user.PasswordHash);
        Assert.NotEqual(oldStamp, user.SecurityStamp);
        Assert.False(user.IsLockedOut(Now));
    }

    [Fact]
    public void AssignRole_Twice_KeepsOneAssignment()
    {
        var user = NewUser();

        user.AssignRole(SystemRole.Customer, Now);
        user.AssignRole(SystemRole.Customer, Now);

        Assert.Single(user.Roles);
        Assert.True(user.HasRole(SystemRole.Customer));
        Assert.False(user.HasRole(SystemRole.SuperAdmin));
    }
}
