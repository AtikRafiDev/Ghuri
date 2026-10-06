using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Ghuri.Application.Tests.Features.Identity;

// Hand-written stand-ins for the ports and repositories: simple lists and
// predictable values instead of SQL Server, PBKDF2 and real randomness -
// so a handler test runs in milliseconds and every value can be checked.

/// <summary>A clock the test controls: move time forward with Now = Now.AddMinutes(16).</summary>
internal sealed class FakeClock(DateTime nowUtc) : TimeProvider
{
    public DateTime Now { get; set; } = nowUtc;

    public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
}

/// <summary>Readable "hashes" ("hashed:secret123") - fine in tests, where speed and clarity matter.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hashed:" + password;

    public bool Verify(string passwordHash, string password) => passwordHash == Hash(password);
}

/// <summary>Predictable tokens: "opaque-1", "opaque-2"... hashed as "HASH(opaque-1)".</summary>
internal sealed class FakeTokenService : ITokenService
{
    private int _count;

    public OpaqueToken? LastCreated { get; private set; }

    public AccessToken CreateAccessToken(User user) => new($"access-for-{user.Id}", DateTime.UnixEpoch);

    public OpaqueToken CreateOpaqueToken()
    {
        var value = $"opaque-{++_count}";
        LastCreated = new OpaqueToken(value, HashOpaqueToken(value));
        return LastCreated;
    }

    public string HashOpaqueToken(string value) => $"HASH({value})";
}

internal sealed class FakeClientInfo : IClientInfo
{
    public string? IpAddress => "127.0.0.1";
    public string? UserAgent => "unit-tests";
}

internal sealed class RecordingEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByPhoneAsync(PhoneNumber phone, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(u => u.PhoneNumber.Equals(phone)));

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(u => u.NormalizedEmail == User.NormalizeEmail(email)));

    public Task<bool> PhoneExistsAsync(PhoneNumber phone, CancellationToken cancellationToken) =>
        Task.FromResult(Users.Exists(u => u.PhoneNumber.Equals(phone)));

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Users.Exists(u => u.NormalizedEmail == User.NormalizeEmail(email)));

    public void Add(User user) => Users.Add(user);
}

internal sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

    public void Add(RefreshToken token) => Tokens.Add(token);

    // Revoke keeps the FIRST revoke time - same as the real
    // "WHERE RevokedAtUtc IS NULL" update.
    public Task RevokeFamilyAsync(Guid familyId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        Tokens.Where(t => t.FamilyId == familyId).ToList().ForEach(t => t.Revoke(nowUtc));
        return Task.CompletedTask;
    }

    public Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        Tokens.Where(t => t.UserId == userId).ToList().ForEach(t => t.Revoke(nowUtc));
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryOtpRepository : IOtpRepository
{
    public List<OtpCode> Codes { get; } = [];

    public Task<OtpCode?> GetLatestAsync(string destination, OtpPurpose purpose, CancellationToken cancellationToken) =>
        Task.FromResult(Codes.LastOrDefault(c => c.Destination == destination && c.Purpose == purpose));

    public Task<int> CountSinceAsync(string destination, OtpPurpose purpose, DateTime sinceUtc, CancellationToken cancellationToken) =>
        Task.FromResult(Codes.Count(c => c.Destination == destination && c.Purpose == purpose && c.CreatedAtUtc >= sinceUtc));

    public void Add(OtpCode code) => Codes.Add(code);
}

/// <summary>Everything an Identity handler needs, wired together, with the same numbers as appsettings.json.</summary>
internal sealed class IdentityTestContext
{
    public const string Password = "correct-password";
    public static readonly DateTime Start = new(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);

    public FakeClock Clock { get; } = new(Start);
    public InMemoryUserRepository Users { get; } = new();
    public InMemoryRefreshTokenRepository RefreshTokens { get; } = new();
    public InMemoryOtpRepository Otps { get; } = new();
    public FakeTokenService Tokens { get; } = new();
    public FakePasswordHasher Passwords { get; } = new();
    public RecordingEmailSender Emails { get; } = new();

    public IOptions<AuthOptions> Settings { get; } = Options.Create(new AuthOptions
    {
        MaxFailedLogins = 5,
        LockoutMinutes = 15,
        RefreshTokenDays = 30,
        PasswordResetLinkMinutes = 30,
        StaffInviteLinkHours = 72,
        MaxResetEmailsPerHour = 5,
        PasswordResetUrl = "http://localhost:5173/reset-password",
    });

    public SessionIssuer Sessions => new(Tokens, RefreshTokens, new FakeClientInfo(), Settings);

    public PasswordLinkSender PasswordLinks => new(Otps, Tokens, Emails, Settings);

    public User AddCustomer(string phone = "01711000000", string email = "rahim@example.com")
    {
        var user = User.Create("Rahim Uddin", PhoneNumber.Create(phone), email, Passwords.Hash(Password));
        user.AssignRole(SystemRole.Customer, Start);
        Users.Add(user);
        return user;
    }
}
