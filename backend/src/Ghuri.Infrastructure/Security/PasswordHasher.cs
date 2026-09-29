using Ghuri.Domain.Entities.Iam;
using Microsoft.AspNetCore.Identity;
using IPasswordHasher = Ghuri.Application.Abstractions.Ports.IPasswordHasher;

namespace Ghuri.Infrastructure.Security;

/// <summary>
/// IPasswordHasher, done by ASP.NET Core Identity's own PasswordHasher:
/// PBKDF2 with HMAC-SHA512, 100,000 iterations and a random salt per
/// password (blueprint section 8: "Identity defaults (PBKDF2)").
/// </summary>
/// <remarks>
/// Why not write our own: password hashing is easy to get subtly wrong
/// (too fast, no salt, timing leaks), and this implementation is
/// maintained and reviewed by Microsoft. We borrow just this one class -
/// none of Identity's EF tables - because iam.Users is our own design.
/// </remarks>
internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    // Identity's API takes a user object, but the default hasher never
    // reads it - so null is passed rather than inventing a fake User.
    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        // "SuccessRehashNeeded" = correct password, but hashed with older,
        // weaker settings. Still a correct password, so it counts as success.
        _inner.VerifyHashedPassword(null!, passwordHash, password) is not PasswordVerificationResult.Failed;
}
