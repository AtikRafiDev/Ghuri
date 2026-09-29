using System.Security.Claims;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Enums;
using Ghuri.Infrastructure.Security;

namespace Ghuri.Api.Authentication;

/// <summary>
/// ICurrentUser, answered from the current HTTP request's logged-in user
/// (blueprint section 9.3: "HttpCurrentUser (Api)"). Lives in Api because
/// only the Api knows there IS an HTTP request.
/// </summary>
/// <remarks>
/// HttpContext.User is filled by the JWT check (UseAuthentication) from the
/// access token's claims. No token, or an invalid one: User is empty,
/// UserId is null, Roles is empty - and audit columns record "system".
/// </remarks>
internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            // "sub" is the standard JWT claim for the user id (we keep claim
            // names as-is: MapInboundClaims = false). NameIdentifier is the
            // name older ASP.NET Core mapping would have renamed it to.
            var raw = User?.FindFirstValue("sub") ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public IReadOnlyList<SystemRole> Roles =>
        User?.FindAll(JwtOptions.RoleClaimType)
            .Select(claim => Enum.TryParse<SystemRole>(claim.Value, out var role) ? role : (SystemRole?)null)
            .OfType<SystemRole>() // silently drops a role name we don't know
            .ToList() ?? [];

    public bool IsInRole(SystemRole role) => Roles.Contains(role);
}
