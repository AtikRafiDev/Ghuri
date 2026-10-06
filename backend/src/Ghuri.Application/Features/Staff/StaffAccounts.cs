using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;

namespace Ghuri.Application.Features.Staff;

/// <summary>Loading a staff account to change it - the one place the Staff commands share.</summary>
internal static class StaffAccounts
{
    /// <summary>
    /// The user, if it is a staff account other than the Super Admin. A
    /// customer's id answers "not found": this page only manages staff.
    /// </summary>
    public static async Task<Result<User>> GetEditableAsync(this IUserRepository users, Guid id, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null || !user.IsStaff)
            return StaffErrors.NotFound;
        if (user.HasRole(SystemRole.SuperAdmin))
            return StaffErrors.SuperAdminProtected;
        return user;
    }

    /// <summary>The staff role of a staff account - for emails and lists.</summary>
    public static SystemRole StaffRole(this User user) =>
        user.HasRole(SystemRole.SuperAdmin) ? SystemRole.SuperAdmin : User.AssignableStaffRoles.First(user.HasRole);
}
