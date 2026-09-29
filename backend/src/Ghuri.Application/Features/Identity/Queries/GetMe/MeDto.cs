namespace Ghuri.Application.Features.Identity.Queries.GetMe;

/// <summary>
/// What the frontend may know about the logged-in user. A DTO, never the
/// User entity itself: the entity also holds PasswordHash, SecurityStamp
/// and lockout fields that must never leave the server (blueprint: "API
/// contracts never expose entities").
/// </summary>
public sealed record MeDto(
    Guid Id,
    string FullName,
    string? Email,
    string Phone,
    bool EmailConfirmed,
    bool PhoneConfirmed,
    IReadOnlyList<string> Roles);
