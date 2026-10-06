using Ghuri.Api.Authentication;
using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.Dashboard.Queries.GetAdminDashboard;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Admin;

/// <summary>The admin home's numbers (17-day plan, Day 12: GetAdminDashboard).</summary>
[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Policy = Policies.AdminArea)]
public sealed class AdminDashboardController(ISender sender) : ControllerBase
{
    /// <summary>Today's bookings, revenue (today / this month), pending payments, refunds to process, upcoming trips.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        (await sender.Send(new GetAdminDashboardQuery(), cancellationToken)).ToActionResult();
}
