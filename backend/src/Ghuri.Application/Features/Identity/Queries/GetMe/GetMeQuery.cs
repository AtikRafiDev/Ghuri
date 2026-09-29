using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Identity.Queries.GetMe;

/// <summary>
/// "Who am I?" - the logged-in user's profile and roles. The frontend asks
/// this right after login and on every page load, to show the name and
/// decide which menus (e.g. Admin) to display.
/// </summary>
/// <remarks>No input - the user comes from the access token (ICurrentUser) - so no validator either.</remarks>
public sealed record GetMeQuery : IQuery<MeDto>;
