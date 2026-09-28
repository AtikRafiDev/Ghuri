namespace Ghuri.Domain.Enums;

/// <summary>
/// The fixed, seeded row ids in iam.Roles (blueprint section 8.3). Having
/// this enum means code can write "SystemRole.SuperAdmin" instead of the
/// magic number 1 when checking or assigning roles.
/// </summary>
public enum SystemRole : byte
{
    SuperAdmin = 1,
    Manager = 2,
    Sales = 3,
    Accounts = 4,
    Customer = 5
}
