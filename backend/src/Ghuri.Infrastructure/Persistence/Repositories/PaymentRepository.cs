using Ghuri.Domain.Entities.Payment;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IPaymentRepository.</summary>
internal sealed class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    // Same trick as BookingRepository: our own constant in the SQL text, "AS [Value]" for EF.
    private const string NextNoSql =
        "SELECT NEXT VALUE FOR [payment].[" + AppDbContext.PaymentNoSequenceName + "] AS [Value]";

    public async Task<string> NextPaymentNoAsync(CancellationToken cancellationToken)
    {
        // ToListAsync, not SingleAsync - see TourPackageRepository.NextPackageCodeAsync.
        var values = await db.Database.SqlQueryRaw<int>(NextNoSql).ToListAsync(cancellationToken);
        return $"PAY{values.Single()}";
    }

    public Task<Payment?> GetByPaymentNoAsync(string paymentNo, CancellationToken cancellationToken) =>
        db.Payments.FirstOrDefaultAsync(p => p.PaymentNo == paymentNo, cancellationToken);

    // UPDLOCK: see BookingRepository.GetByIdForUpdateAsync.
    public async Task<Payment?> GetByPaymentNoForUpdateAsync(string paymentNo, CancellationToken cancellationToken)
    {
        var rows = await db.Payments
            .FromSqlInterpolated($"SELECT * FROM [payment].[Payments] WITH (UPDLOCK, ROWLOCK) WHERE [PaymentNo] = {paymentNo}")
            .ToListAsync(cancellationToken);
        return rows.SingleOrDefault();
    }

    public void Add(Payment payment) => db.Payments.Add(payment);

    public Task<bool> EventExistsAsync(PaymentProvider provider, string providerEventId, CancellationToken cancellationToken) =>
        db.PaymentEvents.AnyAsync(e => e.Provider == provider && e.ProviderEventId == providerEventId, cancellationToken);

    public void AddEvent(PaymentEvent paymentEvent) => db.PaymentEvents.Add(paymentEvent);
}
