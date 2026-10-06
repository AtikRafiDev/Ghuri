using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Staff.Queries.GetStaffUsers;

internal sealed class GetStaffUsersHandler(IReadDbContext db)
    : IQueryHandler<GetStaffUsersQuery, IReadOnlyList<StaffUserDto>>
{
    private static readonly byte[] StaffRoleIds =
        [(byte)SystemRole.SuperAdmin, (byte)SystemRole.Manager, (byte)SystemRole.Sales, (byte)SystemRole.Accounts];

    public async ValueTask<Result<IReadOnlyList<StaffUserDto>>> Handle(GetStaffUsersQuery query, CancellationToken cancellationToken)
    {
        // One SQL query. PasswordHash itself is never read - only whether there is one.
        var rows = await db.Users
            .Where(u => u.Roles.Any(r => StaffRoleIds.Contains(r.RoleId)))
            .OrderBy(u => u.Status).ThenBy(u => u.FullName).ThenBy(u => u.Id)
            .Select(u => new
            {
                u.Id, u.FullName, u.PhoneNumber, u.Email, u.Status, u.LastLoginUtc,
                PasswordSet = u.PasswordHash != null,
                // The lowest id is the strongest role: SuperAdmin 1, Manager 2, Sales 3, Accounts 4.
                RoleId = u.Roles.Where(r => StaffRoleIds.Contains(r.RoleId)).Min(r => r.RoleId)
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new StaffUserDto(
                r.Id, r.FullName, r.PhoneNumber.Value, r.Email, (SystemRole)r.RoleId, r.Status, r.PasswordSet, r.LastLoginUtc,
                Editable: r.RoleId != (byte)SystemRole.SuperAdmin))
            .ToList();
    }
}
