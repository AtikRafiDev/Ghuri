namespace Ghuri.Domain.Enums;

/// <summary>
/// Maps to iam.Users.Status (TINYINT). The blueprint's rule: "values are
/// never renumbered" - once shipped, 1 always means Active, forever, even
/// if we stop using it. Declared as byte to match TINYINT exactly.
/// </summary>
public enum UserStatus : byte
{
    Active = 1,
    Locked = 2,
    Disabled = 3
}
