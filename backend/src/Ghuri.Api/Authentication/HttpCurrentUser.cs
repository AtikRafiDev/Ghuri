using System.Security.Claims;
using Ghuri.Application.Abstractions.Ports;

namespace Ghuri.Api.Authentication;

/// <summary>
/// ICurrentUser, answered from the current HTTP request's logged-in user
/// (blueprint section 9.3: "HttpCurrentUser (Api)"). Lives in Api because
/// only the Api knows there IS an HTTP request.
/// </summary>
/// <remarks>
/// Login doesn't exist yet (Day 2), so today this always returns null -
/// and every audit column records "system". Once JWT login is built, the
/// token's user id claim appears here and gets recorded automatically,
/// with no change needed anywhere else.
/// </remarks>
internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            // "sub" is the standard JWT claim for the user id;
            // NameIdentifier is what ASP.NET Core may map it to.
            var raw = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }
}
