using Ghuri.Api.Authentication;
using Ghuri.Api.ErrorHandling;
using Ghuri.Application.Features.Staff.Commands.ChangeStaffRole;
using Ghuri.Application.Features.Staff.Commands.CreateStaffUser;
using Ghuri.Application.Features.Staff.Commands.DisableStaffUser;
using Ghuri.Application.Features.Staff.Commands.EnableStaffUser;
using Ghuri.Application.Features.Staff.Commands.SendStaffPasswordLink;
using Ghuri.Application.Features.Staff.Queries.GetStaffUsers;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers.Admin;

/// <summary>
/// Staff accounts (Admin → Staff): the Super Admin creates Manager, Sales and
/// Accounts users, changes their role, disables them. Customers sign up
/// themselves at /auth/register; staff are only ever made here.
/// </summary>
/// <remarks>Both policies apply: staff (AdminArea, like every admin controller) AND Super Admin (ManageStaff).</remarks>
[ApiController]
[Route("api/v1/admin/staff")]
[Authorize(Policy = Policies.AdminArea)]
[Authorize(Policy = Policies.ManageStaff)]
public sealed class AdminStaffController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        (await sender.Send(new GetStaffUsersQuery(), cancellationToken)).ToActionResult();

    /// <summary>Creates the account and emails the person a link to set their password.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateStaffUserCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Created((string?)null, new CreatedResponse(result.Value))
            : ResultExtensions.ToProblem(result.Error);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, ChangeStaffRoleCommand command, CancellationToken cancellationToken) =>
        (await sender.Send(command with { Id = id }, cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new DisableStaffUserCommand(id), cancellationToken)).ToActionResult();

    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new EnableStaffUserCommand(id), cancellationToken)).ToActionResult();

    /// <summary>Emails a new "set your password" link (lost welcome email, expired link, forgotten password).</summary>
    [HttpPost("{id:guid}/password-link")]
    public async Task<IActionResult> SendPasswordLink(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new SendStaffPasswordLinkCommand(id), cancellationToken)).ToActionResult();
}
