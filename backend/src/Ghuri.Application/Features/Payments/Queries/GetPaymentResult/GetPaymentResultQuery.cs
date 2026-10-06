using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Application.Features.Identity;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Payments.Queries.GetPaymentResult;

/// <summary>
/// "Is my payment confirmed yet?" (17-day plan, Day 10: GetPaymentResult).
/// The result page asks this every few seconds after SSLCommerz sends the
/// customer back, until the payment is settled. Only the booking's own
/// customer can see it - anyone else gets "not found", like GetMyBooking.
/// </summary>
public sealed record GetPaymentResultQuery(string PaymentNo) : IQuery<PaymentResultDto>;

/// <summary>
/// Status: Pending while we wait for SSLCommerz's confirmation · Succeeded
/// once validated. BookingStatus says what the money did: Confirmed normally,
/// or still Expired / Cancelled when it came too late (a refund follows).
/// </summary>
public sealed record PaymentResultDto(
    string PaymentNo,
    PaymentStatus Status,
    decimal Amount,
    string Currency,
    string BookingNo,
    BookingStatus BookingStatus);

internal sealed class GetPaymentResultHandler(IReadDbContext db, ICurrentUser currentUser)
    : IQueryHandler<GetPaymentResultQuery, PaymentResultDto>
{
    public async ValueTask<Result<PaymentResultDto>> Handle(GetPaymentResultQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } customerId)
            return IdentityErrors.NotAuthenticated;

        var result = await (
                from p in db.Payments
                join b in db.Bookings on p.BookingId equals b.Id
                where p.PaymentNo == query.PaymentNo && b.CustomerId == customerId
                select new PaymentResultDto(p.PaymentNo, p.Status, p.Amount, p.Currency, b.BookingNo, b.Status))
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
            return PaymentErrors.PaymentNotFound;
        return result;
    }
}
