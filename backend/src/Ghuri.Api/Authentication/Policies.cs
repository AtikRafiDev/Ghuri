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

    /// <summary>Cancel a booking for the agency (SuperAdmin, Manager, Sales - the people who talk to customers).</summary>
    public const string CancelBookings = nameof(CancelBookings);

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
                nameof(SystemRole.SuperAdmin), nameof(SystemRole.Manager), nameof(SystemRole.Sales)));

        return services;
    }
}
