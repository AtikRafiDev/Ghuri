using Ghuri.Domain.Entities.Payment;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Exceptions;

namespace Ghuri.Domain.Tests.Entities.Payment;

/// <summary>
/// A refund's life (Day 12), by hand or through SSLCommerz. Money: it's sent
/// once or rejected once - never both, never twice, never by hand while SSLCommerz sends it.
/// </summary>
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

    // ---------- Through SSLCommerz ----------

    [Fact]
    public void SentToSslCommerz_IsProcessing_StillOpen_WithItsRefundId()
    {
        var refund = NewRefund();

        refund.MarkSentToGateway("59bd63fea5455", Staff, Now);

        Assert.Equal((RefundStatus.Processing, "59bd63fea5455", (Guid?)Staff, true, true, (DateTime?)null),
            (refund.Status, refund.ProviderRefundId, refund.ApprovedBy, refund.IsOpen, refund.IsWithGateway, refund.CompletedAtUtc));
    }

    [Fact]
    public void WhileSslCommerzSendsIt_StaffCantCompleteRejectOrSendItAgain()
    {
        // Money: sending it by hand too, or a second gateway refund, pays the customer twice.
        var refund = NewRefund();
        refund.MarkSentToGateway("ref-1", Staff, Now);

        Assert.Equal("refund_in_progress", Assert.Throws<DomainException>(() => refund.MarkCompleted("BK-1", Staff, Now)).Code);
        Assert.Equal("refund_in_progress", Assert.Throws<DomainException>(() => refund.Reject("x", Staff, Now)).Code);
        Assert.Equal("refund_in_progress", Assert.Throws<DomainException>(() => refund.MarkSentToGateway("ref-2", Staff, Now)).Code);
        Assert.Equal("ref-1", refund.ProviderRefundId);
    }

    [Fact]
    public void SslCommerzSaysRefunded_CompletesIt()
    {
        var refund = NewRefund();
        refund.MarkSentToGateway("ref-1", Staff, Now);

        refund.MarkGatewayRefunded(Now.AddHours(5));

        Assert.Equal((RefundStatus.Completed, (DateTime?)Now.AddHours(5), false), (refund.Status, refund.CompletedAtUtc, refund.IsOpen));
    }

    [Fact]
    public void OnlyARefundWithSslCommerz_CanBeCompletedBySslCommerz()
    {
        var refund = NewRefund();

        Assert.Equal("refund_not_with_gateway", Assert.Throws<DomainException>(() => refund.MarkGatewayRefunded(Now)).Code);
    }

    [Fact]
    public void AFailedRefund_IsStillOwed_AndCanBeSentAgainOrByHand()
    {
        var refund = NewRefund();
        refund.MarkSentToGateway("ref-1", Staff, Now);

        refund.MarkGatewayFailed("SSLCommerz cancelled the refund.");

        Assert.Equal((RefundStatus.Failed, true, false, "SSLCommerz cancelled the refund."),
            (refund.Status, refund.IsOpen, refund.IsWithGateway, refund.FailureReason));

        refund.MarkSentToGateway("ref-2", Staff, Now); // a new try...
        Assert.Equal((RefundStatus.Processing, "ref-2", (string?)null), (refund.Status, refund.ProviderRefundId, refund.FailureReason)); // ...without the old reason
    }

    [Fact]
    public void AFailedRefund_CanBeSentByHand()
    {
        var refund = NewRefund();
        refund.MarkGatewayFailed("SSLCommerz refused the refund: Invalid bank tran id.");

        refund.MarkCompleted("BK-1", Staff, Now);

        Assert.Equal(RefundStatus.Completed, refund.Status);
    }

    [Fact]
    public void AClosedRefund_CantFailAnyMore()
    {
        var refund = NewRefund();
        refund.MarkCompleted("BK-1", Staff, Now);

        Assert.Equal("refund_not_open", Assert.Throws<DomainException>(() => refund.MarkGatewayFailed("late")).Code);
        Assert.Equal(RefundStatus.Completed, refund.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ARefundOfNothing_IsRejected(decimal amount) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Refund.Request("RF1001", Guid.NewGuid(), Guid.NewGuid(), amount, 100, "x", null));
}
