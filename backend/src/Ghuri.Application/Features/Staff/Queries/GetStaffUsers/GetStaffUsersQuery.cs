using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Domain.Enums;

namespace Ghuri.Application.Features.Staff.Queries.GetStaffUsers;

/// <summary>
/// The Staff page's table: every account with an admin-panel role, the
/// Super Admin included (shown, but never editable). Customers are not
/// listed. A short list, so not paged. Active first, then by name.
/// </summary>
public sealed record GetStaffUsersQuery : IQuery<IReadOnlyList<StaffUserDto>>;

/// <summary>One row of the Staff table.</summary>
/// <remarks>
/// Role: SuperAdmin, Manager, Sales or Accounts. PasswordSet false = they
/// haven't used the welcome link yet. Editable false = the Super Admin.
/// </remarks>
public sealed record StaffUserDto(
    Guid Id,
    string FullName,
    string Phone,
    string? Email,
    SystemRole Role,
    UserStatus Status,
    bool PasswordSet,
    DateTime? LastLoginUtc,
    bool Editable);
