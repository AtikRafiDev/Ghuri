namespace Ghuri.Domain.Entities.Iam;

/// <summary>
/// Which role(s) a user has. A plain join entity - composite primary key
/// of (UserId, RoleId), no surrogate Guid id of its own, matching the
/// blueprint's iam.UserRoles table exactly.
/// </summary>
/// <remarks>
/// Belongs to the User aggregate: created only through User.AssignRole
/// (hence the internal factory), so no code outside Domain can attach a
/// role to a user without going through the User's own rules.
/// </remarks>
public sealed class UserRole
{
    public Guid UserId { get; private set; }
    public byte RoleId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    private UserRole()
    {
    }

    internal static UserRole Create(Guid userId, byte roleId, DateTime assignedAtUtc) =>
        new() { UserId = userId, RoleId = roleId, AssignedAtUtc = assignedAtUtc };
}
