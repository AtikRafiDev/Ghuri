using Ghuri.Domain.Entities.Payment;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IRefundRepository.</summary>
internal sealed class RefundRepository(AppDbContext db) : IRefundRepository
{
    // Same trick as PaymentRepository: our own constant in the SQL text, "AS [Value]" for EF.
    private const string NextNoSql =
        "SELECT NEXT VALUE FOR [payment].[" + AppDbContext.RefundNoSequenceName + "] AS [Value]";

    public async Task<string> NextRefundNoAsync(CancellationToken cancellationToken)
    {
        var values = await db.Database.SqlQueryRaw<int>(NextNoSql).ToListAsync(cancellationToken);
        return $"RF{values.Single()}";
    }

    // UPDLOCK: see BookingRepository.GetByIdForUpdateAsync.
    public async Task<Refund?> GetByRefundNoForUpdateAsync(string refundNo, CancellationToken cancellationToken)
    {
        var rows = await db.Refunds
            .FromSqlInterpolated($"SELECT * FROM [payment].[Refunds] WITH (UPDLOCK, ROWLOCK) WHERE [RefundNo] = {refundNo}")
            .ToListAsync(cancellationToken);
        return rows.SingleOrDefault();
    }

    public void Add(Refund refund) => db.Refunds.Add(refund);
}
