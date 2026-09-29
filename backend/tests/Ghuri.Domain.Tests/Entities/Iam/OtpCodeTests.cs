using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;

namespace Ghuri.Domain.Tests.Entities.Iam;

public class OtpCodeTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);

    // A password-reset link: valid 30 minutes.
    private static OtpCode NewResetLink() =>
        OtpCode.Create("admin@ghuri.local", OtpPurpose.ResetPassword, new string('a', 64), Now, Now.AddMinutes(30));

    [Fact]
    public void IsUsable_UntilTheDeadline_ThenExpired()
    {
        var code = NewResetLink();

        Assert.True(code.IsUsable(Now.AddMinutes(29)));
        Assert.False(code.IsUsable(Now.AddMinutes(30)));
        Assert.True(code.IsExpired(Now.AddMinutes(30)));
    }

    [Fact]
    public void FiveWrongAttempts_KillTheCode_BeforeItExpires()
    {
        var code = NewResetLink();

        for (var i = 0; i < 4; i++)
            code.RecordWrongAttempt();
        Assert.True(code.IsUsable(Now));

        code.RecordWrongAttempt();
        Assert.False(code.IsUsable(Now));
    }

    [Fact]
    public void RecordWrongAttempt_NeverGoesAboveTheDatabaseLimit()
    {
        var code = NewResetLink();

        for (var i = 0; i < 10; i++)
            code.RecordWrongAttempt();

        Assert.Equal(OtpCode.MaxAttempts, code.Attempts);
    }

    [Fact]
    public void Consume_WorksOnlyOnce()
    {
        var code = NewResetLink();

        code.Consume(Now);

        Assert.False(code.IsUsable(Now));
        var error = Assert.Throws<DomainException>(() => code.Consume(Now));
        Assert.Equal("otp_not_usable", error.Code);
    }

    [Fact]
    public void Consume_AfterTheDeadline_IsRefused()
    {
        var code = NewResetLink();

        Assert.Throws<DomainException>(() => code.Consume(Now.AddMinutes(31)));
    }
}
