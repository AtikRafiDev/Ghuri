namespace Ghuri.Domain.Enums;

/// <summary>Maps to iam.OtpCodes.Purpose (TINYINT).</summary>
public enum OtpPurpose : byte
{
    Login = 1,
    VerifyPhone = 2,
    ResetPassword = 3
}
