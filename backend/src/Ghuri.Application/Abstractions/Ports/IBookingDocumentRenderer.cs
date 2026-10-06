using Ghuri.Domain.Enums;

namespace Ghuri.Application.Abstractions.Ports;

/// <summary>
/// Turns a booking into its PDFs (17-day plan, Day 11: "QuestPDF invoice +
/// e-voucher"). Application decides WHAT goes on them (BookingDocumentData);
/// Infrastructure decides how it looks - QuestPDF is never seen here.
/// </summary>
public interface IBookingDocumentRenderer
{
    /// <summary>What was charged and paid - the customer's receipt.</summary>
    byte[] RenderInvoice(BookingDocumentData booking);

    /// <summary>The proof of booking the customer shows on the trip: dates, nights, who's travelling.</summary>
    byte[] RenderVoucher(BookingDocumentData booking);
}

/// <summary>Everything the invoice and the voucher show, read once (BookingDocumentLoader) and given to the renderer.</summary>
public sealed record BookingDocumentData(
    string BookingNo,
    BookingType BookingType,
    BookingStatus Status,
    string TripTitle,
    DateOnly StartDate,
    DateOnly EndDate,
    int Nights,
    string ContactName,
    string ContactPhone,
    string? ContactEmail,
    string? SpecialRequest,
    IReadOnlyList<DocumentTraveller> Travellers,
    IReadOnlyList<DocumentPriceLine> PriceLines,
    decimal TotalAmount,
    decimal PaidAmount,
    string Currency,
    IReadOnlyList<DocumentPayment> Payments,
    DateOnly IssuedOn);

public sealed record DocumentTraveller(string FullName, TravellerType Type, bool IsLead);

/// <summary>One invoice line, e.g. ("Adult", 2, 12000, 24000). A discount is a line with a negative Amount.</summary>
public sealed record DocumentPriceLine(string Description, int Quantity, decimal UnitPrice, decimal Amount);

public sealed record DocumentPayment(string PaymentNo, string? Method, DateTime PaidAtUtc, decimal Amount);
