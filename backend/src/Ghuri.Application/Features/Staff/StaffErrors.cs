using Ghuri.Application.Common;

namespace Ghuri.Application.Features.Staff;

/// <summary>
/// Every expected failure of the Staff page. The Code is what the frontend
/// switches on, so it must never change once shipped. A phone or email
/// that already has an account reuses IdentityErrors.PhoneTaken / EmailTaken.
/// </summary>
public static class StaffErrors
{
    /// <summary>No such user - or a customer account, which this page never touches.</summary>
    public static readonly Error NotFound =
        Error.NotFound("staff_not_found", "This staff account doesn't exist.");

    public static readonly Error SuperAdminProtected =
        Error.Conflict("super_admin_protected", "The Super Admin account can't be changed here.");

    public static readonly Error AccountDisabled =
        Error.Conflict("staff_disabled", "This account is disabled. Enable it before sending a password link.");
}
