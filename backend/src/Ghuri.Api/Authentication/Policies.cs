using Ghuri.Domain.Enums;

namespace Ghuri.Api.Authentication;

/// <summary>
/// Policy names as constants (blueprint section 14: "no magic strings for
/// roles or policies"). Endpoints use [Authorize(Policy = Policies.AdminArea)]
/// - a typo becomes a compile error instead of a silently open endpoint.
/// </summary>
public static class Policies
{
    /// <summary>Any staff member: the admin panel's API (blueprint: /api/v1/admin/...).</summary>
    public const string AdminArea = nameof(AdminArea);

    /// <summary>Customer-only endpoints: bookings, wishlist, reviews (blueprint: /api/v1/me/...).</summary>
    public const string Customer = nameof(Customer);

    /// <summary>
    /// Actions that move money: record a manual payment, mark a refund as sent
    /// or reject it (decided 2026-10-06: SuperAdmin, Manager, Accounts - never Sales).
    /// </summary>
    public const string ManageMoney = nameof(ManageMoney);

    /// <summary>
    /// Cancel a booking for the agency. SuperAdmin and Manager (2026-10-08:
    /// Sales no longer sees bookings, and Accounts handles money, not customers' trips).
    /// </summary>
    public const string CancelBookings = nameof(CancelBookings);

    /// <summary>Quote or reject a custom trip (Day 13) - the same customer-facing people as CancelBookings.</summary>
    public const string QuoteTrips = nameof(QuoteTrips);

    /// <summary>
    /// Create staff accounts, change their role, disable them (Admin → Staff).
    /// Super Admin only: whoever can hand out roles can give themselves any power.
    /// </summary>
    public const string ManageStaff = nameof(ManageStaff);

    // Which admin sections each role may open (decided 2026-10-08). Each one
    // sits on top of AdminArea; custom trips stay open to every staff member.

    /// <summary>The admin dashboard (sales figures, every booking at a glance): Super Admin only.</summary>
    public const string ViewDashboard = nameof(ViewDashboard);

    /// <summary>The bookings, payments and refunds lists: SuperAdmin, Manager, Accounts - never Sales.</summary>
    public const string ViewBookings = nameof(ViewBookings);

    /// <summary>Packages, departures, destinations, categories and their photos: SuperAdmin, Manager, Sales - never Accounts.</summary>
    public const string ManageCatalogue = nameof(ManageCatalogue);

    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        // Role names come from SystemRole, the same enum the token's role
        // claims are written from - one source for both sides.
        services.AddAuthorizationBuilder()
            .AddPolicy(AdminArea, policy => policy.RequireRole(
                nameof(SystemRole.SuperAdmin), nameof(SystemRole.Manager),
                nameof(SystemRole.Sales), nameof(SystemRole.Accounts)))
            .AddPolicy(Customer, policy => policy.RequireRole(nameof(SystemRole.Customer)))
            .AddPolicy(ManageMoney, policy => policy.RequireRole(
                nameof(SystemRole.SuperAdmin), nameof(SystemRole.Manager), nameof(SystemRole.Accounts)))
            .AddPolicy(CancelBookings, policy => policy.RequireRole(
                nameof(SystemRole.SuperAdmin), nameof(SystemRole.Manager)))
            .AddPolicy(QuoteTrips, policy => policy.RequireRole(
                nameof(SystemRole.SuperAdmin), nameof(SystemRole.Manager), nameof(SystemRole.Sales)))
            .AddPolicy(ManageStaff, policy => policy.RequireRole(nameof(SystemRole.SuperAdmin)))
            .AddPolicy(ViewDashboard, policy => policy.RequireRole(nameof(SystemRole.SuperAdmin)))
            .AddPolicy(ViewBookings, policy => policy.RequireRole(
                nameof(SystemRole.SuperAdmin), nameof(SystemRole.Manager), nameof(SystemRole.Accounts)))
            .AddPolicy(ManageCatalogue, policy => policy.RequireRole(
                nameof(SystemRole.SuperAdmin), nameof(SystemRole.Manager), nameof(SystemRole.Sales)));

        return services;
    }
}
