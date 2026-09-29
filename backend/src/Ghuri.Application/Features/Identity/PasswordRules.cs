using FluentValidation;

namespace Ghuri.Application.Features.Identity;

/// <summary>
/// The ONE definition of an acceptable new password, shared by
/// RegisterCustomer, ResetPassword and ChangePassword.
/// </summary>
/// <remarks>
/// Length only - no "must contain a symbol" rules. Current guidance
/// (NIST SP 800-63B) is that length protects far better than forced
/// symbols, which mostly produce "Password1!". Minimum 8 per the blueprint
/// (section 8). The maximum stops someone sending a 10 MB "password" to
/// make the (deliberately slow) hasher burn CPU.
/// </remarks>
internal static class PasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static IRuleBuilderOptions<T, string> ValidNewPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MinimumLength(MinLength)
            .MaximumLength(MaxLength);
}
