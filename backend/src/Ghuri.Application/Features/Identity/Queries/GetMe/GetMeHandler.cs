using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Identity.Queries.GetMe;

/// <summary>
/// A QUERY handler: reads through IReadDbContext (no tracking, no
/// repository) and selects only the columns the DTO needs (blueprint
/// section 8: "project straight into DTOs").
/// </summary>
internal sealed class GetMeHandler(IReadDbContext db, ICurrentUser currentUser) : IQueryHandler<GetMeQuery, MeDto>
{
    public async ValueTask<Result<MeDto>> Handle(GetMeQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
            return IdentityErrors.NotAuthenticated;

        // Becomes ONE SQL query: exactly these columns, plus the role ids.
        // PasswordHash & co. are never even read from the database.
        var me = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.EmailConfirmed,
                u.PhoneConfirmed,
                RoleIds = u.Roles.Select(r => r.RoleId).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        // Token still valid, but the account was deleted meanwhile.
        if (me is null)
            return IdentityErrors.NotAuthenticated;

        return new MeDto(
            me.Id, me.FullName, me.Email, me.PhoneNumber.Value, me.EmailConfirmed, me.PhoneConfirmed,
            me.RoleIds.Select(id => ((SystemRole)id).ToString()).ToList());
    }
}
