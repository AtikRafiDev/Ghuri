using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IOtpRepository.</summary>
/// <remarks>
/// destination is matched exactly as stored - handlers pass an already
/// normalized value (User.NormalizeEmail, or PhoneNumber.Value), the same
/// one they used when creating the code.
/// </remarks>
internal sealed class OtpRepository(AppDbContext db) : IOtpRepository
{
    public Task<OtpCode?> GetLatestAsync(string destination, OtpPurpose purpose, CancellationToken cancellationToken) =>
        db.OtpCodes
            .Where(o => o.Destination == destination && o.Purpose == purpose)
            // Served by the (Destination, CreatedAtUtc) index. Id breaks a
            // tie if two codes were created in the same millisecond.
            .OrderByDescending(o => o.CreatedAtUtc)
            .ThenByDescending(o => o.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> CountSinceAsync(string destination, OtpPurpose purpose, DateTime sinceUtc, CancellationToken cancellationToken) =>
        db.OtpCodes.CountAsync(
            o => o.Destination == destination && o.Purpose == purpose && o.CreatedAtUtc >= sinceUtc,
            cancellationToken);

    public void Add(OtpCode code) => db.OtpCodes.Add(code);
}
