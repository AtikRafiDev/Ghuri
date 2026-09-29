using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Repositories;

/// <summary>
/// Loads and adds User aggregates for COMMAND handlers (blueprint section
/// 7.1). Every Get returns the user together with their Roles - login needs
/// them for the token, and they're only a row or two.
/// </summary>
/// <remarks>
/// Lives in Domain, implemented in Infrastructure: the Domain decides what
/// it needs from storage, without knowing EF Core exists.
/// There is no Update or Save method on purpose: change the loaded User
/// through its own methods, and TransactionBehavior saves everything once
/// the command succeeds. Read-only screens (e.g. an admin user list) don't
/// use this - queries read through IReadDbContext instead.
/// </remarks>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> GetByPhoneAsync(PhoneNumber phone, CancellationToken cancellationToken);

    /// <summary>Case-insensitive: "Rahim@Mail.com" finds "rahim@mail.com".</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> PhoneExistsAsync(PhoneNumber phone, CancellationToken cancellationToken);

    /// <summary>Case-insensitive, like GetByEmailAsync.</summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    void Add(User user);
}
