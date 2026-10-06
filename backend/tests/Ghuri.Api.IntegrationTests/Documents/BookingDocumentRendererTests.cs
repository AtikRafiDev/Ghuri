using System.Text;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Enums;
using Ghuri.Infrastructure.Documents;
using Microsoft.Extensions.Options;

namespace Ghuri.Api.IntegrationTests.Documents;

/// <summary>
/// The QuestPDF invoice and e-voucher, rendered for real (no database).
/// They must ALWAYS render - a crash here means a customer without a voucher -
/// whatever the names look like and however many travellers there are.
/// </summary>
public class BookingDocumentRendererTests
{
    private static readonly QuestPdfBookingDocumentRenderer Renderer = new(Options.Create(new AgencyOptions
    {
        Name = "Ghuri Tours & Travels",
        Address = "Dhaka, Bangladesh",
        Phone = "01700000000",
        Email = "hello@ghuri.local"
    }));

    private static BookingDocumentData Booking(
        IReadOnlyList<DocumentTraveller>? travellers = null, BookingStatus status = BookingStatus.Confirmed) =>
        new(
            "TB100001", BookingType.FlexibleStay, status, "Cox's Bazar Beach Escape",
            new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 24), 4,
            "Rahim Uddin", "01712345678", "rahim@example.com", "Sea-facing room, please",
            travellers ??
            [
                new DocumentTraveller("Rahim Uddin", TravellerType.Adult, true),
                new DocumentTraveller("Karima Begum", TravellerType.Adult, false),
                new DocumentTraveller("Ayaan", TravellerType.Child, false),
                new DocumentTraveller("Mim", TravellerType.Infant, false)
            ],
            [
                new DocumentPriceLine("Adult", 2, 14_000, 28_000),
                new DocumentPriceLine("Child", 1, 14_000, 14_000),
                new DocumentPriceLine("Infant", 1, 0, 0)
            ],
            42_000, 42_000, "BDT",
            [new DocumentPayment("PAY100001", "BKASH-BKash", new DateTime(2026, 10, 6, 8, 30, 0, DateTimeKind.Utc), 42_000)],
            new DateOnly(2026, 10, 6));

    private static void AssertIsPdf(byte[] pdf)
    {
        Assert.True(pdf.Length > 1_000, $"Only {pdf.Length} bytes - that's no real PDF.");
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public void TheVoucher_IsAPdf() => AssertIsPdf(Renderer.RenderVoucher(Booking()));

    [Fact]
    public void TheInvoice_IsAPdf() => AssertIsPdf(Renderer.RenderInvoice(Booking()));

    [Fact]
    public void ACancelledBookingsInvoice_StillRenders() =>
        AssertIsPdf(Renderer.RenderInvoice(Booking(status: BookingStatus.Cancelled)));

    [Fact]
    public void NamesInBangla_AndOddCharacters_NeverStopTheVoucher()
    {
        var pdf = Renderer.RenderVoucher(Booking(
        [
            new DocumentTraveller("রহিম উদ্দিন", TravellerType.Adult, true),
            new DocumentTraveller("Zoë O'Brien-Ünal ✈", TravellerType.Adult, false)
        ]));

        AssertIsPdf(pdf);
    }

    [Fact]
    public void TwentyTravellers_FlowOntoMorePages()
    {
        var many = Enumerable.Range(1, 20)
            .Select(i => new DocumentTraveller($"Traveller number {i}", TravellerType.Adult, i == 1))
            .ToList();

        AssertIsPdf(Renderer.RenderVoucher(Booking(many)));
    }
}
