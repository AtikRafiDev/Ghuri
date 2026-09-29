using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IUserRepository (blueprint section 7).</summary>
/// <remarks>
/// Loads users WITH tracking (the opposite of the read side): a command
/// handler changes the user it loaded, and EF Core notices those changes
/// when TransactionBehavior calls SaveChanges. That's why there is no
/// Update method - nothing needs to be "sent back".
/// </remarks>
internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    /// <summary>Every lookup brings the roles along - the access token needs them.</summary>
    private IQueryable<User> UsersWithRoles => db.Users.Include(u => u.Roles);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        UsersWithRoles.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByPhoneAsync(PhoneNumber phone, CancellationToken cancellationToken) =>
        UsersWithRoles.FirstOrDefaultAsync(u => u.PhoneNumber == phone, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        // Search the indexed, upper-cased copy - never Email itself - so
        // the lookup is case-insensitive AND uses the index.
        var normalizedEmail = User.NormalizeEmail(email);
        return UsersWithRoles.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<bool> PhoneExistsAsync(PhoneNumber phone, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.PhoneNumber == phone, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        return db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public void Add(User user) => db.Users.Add(user);
}
