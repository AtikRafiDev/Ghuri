using Ghuri.Domain.Entities.Payment;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;

namespace Ghuri.Domain.Tests.Entities.Payment;

/// <summary>A refund's life (Day 12). Money: it's sent once or rejected once - never both, never twice.</summary>
public class RefundTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 6, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Staff = Guid.NewGuid();

    private static Refund NewRefund(Guid? requestedBy = null) =>
        Refund.Request("RF1001", Guid.NewGuid(), Guid.NewGuid(), 17_000, 50, "Cancelled by the customer.", requestedBy);

    [Fact]
    public void ANewRefund_IsRequested_AndOpen()
    {
        var refund = NewRefund();

        Assert.Equal((RefundStatus.Requested, true, (Guid?)null), (refund.Status, refund.IsOpen, refund.RequestedBy)); // null = the system asked
    }

    [Fact]
    public void MarkCompleted_KeepsTheReference_WhoAndWhen()
    {
        var refund = NewRefund();

        refund.MarkCompleted("  BK-REF-551 ", Staff, Now);

        Assert.Equal((RefundStatus.Completed, "BK-REF-551", (Guid?)Staff, (DateTime?)Now, false),
            (refund.Status, refund.ProviderRefundId, refund.ApprovedBy, refund.CompletedAtUtc, refund.IsOpen));
    }

    [Fact]
    public void ARefundSentOnce_CantBeSentAgain_OrRejected()
    {
        var refund = NewRefund();
        refund.MarkCompleted("BK-1", Staff, Now);

        Assert.Equal("refund_not_open", Assert.Throws<DomainException>(() => refund.MarkCompleted("BK-2", Staff, Now)).Code);
        Assert.Equal("refund_not_open", Assert.Throws<DomainException>(() => refund.Reject("Oops", Staff, Now)).Code);
        Assert.Equal("BK-1", refund.ProviderRefundId);
    }

    [Fact]
    public void Reject_KeepsTheReason_AndClosesIt()
    {
        var refund = NewRefund();

        refund.Reject("Already refunded in cash", Staff, Now);

        Assert.Equal((RefundStatus.Rejected, "Already refunded in cash", (DateTime?)null), (refund.Status, refund.FailureReason, refund.CompletedAtUtc));
        Assert.Equal("refund_not_open", Assert.Throws<DomainException>(() => refund.MarkCompleted("BK-1", Staff, Now)).Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ARefundOfNothing_IsRejected(decimal amount) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Refund.Request("RF1001", Guid.NewGuid(), Guid.NewGuid(), amount, 100, "x", null));
}
