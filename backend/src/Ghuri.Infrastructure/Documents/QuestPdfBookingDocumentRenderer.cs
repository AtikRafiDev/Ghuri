using System.Globalization;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Enums;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ghuri.Infrastructure.Documents;

/// <summary>
/// The invoice and the e-voucher as A4 PDFs, drawn with QuestPDF (17-day
/// plan, Day 11). Layout only - WHAT is on them comes from Application
/// (BookingDocumentData).
/// </summary>
/// <remarks>
/// <para>
/// Fonts: Lato (comes with QuestPDF) for Latin text, then "Nirmala UI"
/// (Windows) or "Noto Sans Bengali" (install it on a Linux server) for a
/// name typed in Bangla. Amounts say "BDT" rather than "৳" so they never
/// depend on a fallback font.
/// </para>
/// <para>
/// Licence: QuestPDF's free Community licence covers a business with under
/// US$1M yearly revenue (questpdf.com/license).
/// </para>
/// </remarks>
internal sealed class QuestPdfBookingDocumentRenderer(IOptions<AgencyOptions> options) : IBookingDocumentRenderer
{
    // QuestPDF's global settings, set once before the first PDF - here, so
    // they're in place however the renderer is created (app or test).
    static QuestPdfBookingDocumentRenderer()
    {
        // Without a licence choice QuestPDF refuses to render.
        QuestPDF.Settings.License = LicenseType.Community;

        // A character no font has (a rare symbol in a name) is drawn as an
        // empty box instead of throwing - a voucher with one odd letter beats
        // no voucher at all. The same for a font family that isn't there:
        // the next one in FontFamilies is used.
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;

        // QuestPDF only knows Lato unless allowed to use the machine's fonts:
        // on Windows that brings "Nirmala UI" for names typed in Bangla.
        // A Linux server has no Bengali font - before go-live, ship
        // NotoSansBengali-Regular.ttf next to the app (QuestPDF picks up font
        // files from the app folder by itself) or install fonts-noto.
        QuestPDF.Settings.UseSystemFonts = true;
    }

    private static readonly string[] FontFamilies = ["Lato", "Nirmala UI", "Noto Sans Bengali"];
    private static readonly string Accent = Colors.Blue.Darken3;
    private static readonly string Muted = Colors.Grey.Darken1;
    private static readonly string Line = Colors.Grey.Lighten2;

    public byte[] RenderInvoice(BookingDocumentData booking) =>
        Render("INVOICE", $"INV-{booking.BookingNo}", booking, content => InvoiceContent(content, booking));

    public byte[] RenderVoucher(BookingDocumentData booking) =>
        Render("E-VOUCHER", booking.BookingNo, booking, content => VoucherContent(content, booking));

    // ---------- The page around both ----------

    private byte[] Render(string title, string number, BookingDocumentData booking, Action<ColumnDescriptor> content)
    {
        var agency = options.Value;

        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(style => style.FontSize(10).FontFamily(FontFamilies));

            page.Header().Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(agency.Name).FontSize(18).SemiBold().FontColor(Accent);
                    foreach (var line in new[] { agency.Address, agency.Phone, agency.Email, agency.Website })
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            column.Item().Text(line).FontColor(Muted);
                    }
                });
                row.ConstantItem(180).AlignRight().Column(column =>
                {
                    column.Item().AlignRight().Text(title).FontSize(18).SemiBold();
                    column.Item().AlignRight().Text(number).FontSize(11);
                    column.Item().AlignRight().Text($"Issued {Date(booking.IssuedOn)}").FontColor(Muted);
                });
            });

            page.Content().PaddingVertical(20).Column(column =>
            {
                column.Spacing(16);
                content(column);
            });

            page.Footer().Row(row =>
            {
                row.RelativeItem().Text($"{agency.Name} · Booking {booking.BookingNo}").FontSize(8).FontColor(Muted);
                row.ConstantItem(80).AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(8).FontColor(Muted));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        })).GeneratePdf();
    }

    // ---------- Voucher ----------

    private static void VoucherContent(ColumnDescriptor column, BookingDocumentData booking)
    {
        column.Item().Background(Colors.Green.Lighten4).Padding(10).Row(row =>
        {
            row.RelativeItem().Text("BOOKING CONFIRMED").SemiBold().FontColor(Colors.Green.Darken3);
            row.AutoItem().Text($"Booking no. {booking.BookingNo}").SemiBold();
        });

        column.Item().Element(c => TripBlock(c, booking));

        // A custom trip: every stop with its own dates (Day 15: "voucher shows all legs").
        if (booking.Stops is { Count: > 0 } stops)
        {
            column.Item().Column(route =>
            {
                route.Spacing(4);
                route.Item().Text("Your stops").FontSize(12).SemiBold();
                route.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(24);
                        c.RelativeColumn(3);
                        c.RelativeColumn(3);
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                    });
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("#");
                        header.Cell().Element(HeaderCell).Text("Destination");
                        header.Cell().Element(HeaderCell).Text("Dates");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Nights");
                        header.Cell().Element(HeaderCell).PaddingLeft(16).Text("Then by");
                    });
                    foreach (var stop in stops)
                    {
                        table.Cell().Element(BodyCell).Text(stop.Sequence.ToString(CultureInfo.InvariantCulture));
                        table.Cell().Element(BodyCell).Text(stop.Destination);
                        table.Cell().Element(BodyCell).Text($"{Date(stop.CheckIn)} – {Date(stop.CheckOut)}");
                        table.Cell().Element(BodyCell).AlignRight().Text(stop.Nights.ToString(CultureInfo.InvariantCulture));
                        table.Cell().Element(BodyCell).PaddingLeft(16).Text(stop.TransferToNext ?? "-");
                    }
                });
            });
        }

        column.Item().Column(travellers =>
        {
            travellers.Spacing(4);
            travellers.Item().Text("Travellers").FontSize(12).SemiBold();
            travellers.Item().Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(24);
                    c.RelativeColumn(4);
                    c.RelativeColumn(2);
                });
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("#");
                    header.Cell().Element(HeaderCell).Text("Name");
                    header.Cell().Element(HeaderCell).Text("Type");
                });
                var number = 1;
                foreach (var traveller in booking.Travellers)
                {
                    table.Cell().Element(BodyCell).Text((number++).ToString(CultureInfo.InvariantCulture));
                    table.Cell().Element(BodyCell).Text(traveller.IsLead ? $"{traveller.FullName} (lead)" : traveller.FullName);
                    table.Cell().Element(BodyCell).Text(TravellerType(traveller.Type));
                }
            });
        });

        column.Item().Element(c => ContactBlock(c, "Contact", booking));

        if (!string.IsNullOrWhiteSpace(booking.SpecialRequest))
        {
            column.Item().Column(request =>
            {
                request.Item().Text("Special request").SemiBold();
                request.Item().Text(booking.SpecialRequest);
            });
        }

        column.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(notes =>
        {
            notes.Spacing(2);
            notes.Item().Text("Please note").SemiBold();
            notes.Item().Text("• Show this voucher (printed or on your phone) with a photo ID at the start of the trip.");
            notes.Item().Text("• The lead traveller is the contact for this booking.");
            notes.Item().Text("• Cancellations follow our cancellation policy; refunds are worked out from the days left before the trip.");
        });
    }

    // ---------- Invoice ----------

    private static void InvoiceContent(ColumnDescriptor column, BookingDocumentData booking)
    {
        column.Item().Row(row =>
        {
            row.RelativeItem().Element(c => ContactBlock(c, "Bill to", booking));
            row.ConstantItem(16);
            row.RelativeItem().Element(c => TripBlock(c, booking));
        });

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(4);
                c.RelativeColumn(1);
                c.RelativeColumn(2);
                c.RelativeColumn(2);
            });
            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Description");
                header.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                header.Cell().Element(HeaderCell).AlignRight().Text("Unit price");
                header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
            });
            foreach (var line in booking.PriceLines)
            {
                table.Cell().Element(BodyCell).Text(line.Description);
                table.Cell().Element(BodyCell).AlignRight().Text(line.Quantity.ToString(CultureInfo.InvariantCulture));
                table.Cell().Element(BodyCell).AlignRight().Text(Money(line.UnitPrice, booking.Currency));
                table.Cell().Element(BodyCell).AlignRight().Text(Money(line.Amount, booking.Currency));
            }
        });

        var balance = booking.TotalAmount - booking.PaidAmount;
        column.Item().AlignRight().Width(240).Column(totals =>
        {
            totals.Spacing(3);
            TotalRow(totals, "Total", Money(booking.TotalAmount, booking.Currency), bold: true);
            TotalRow(totals, "Paid", Money(booking.PaidAmount, booking.Currency), bold: false);
            TotalRow(totals, "Balance due", Money(balance > 0 ? balance : 0, booking.Currency), bold: true);
        });

        if (booking.Payments.Count > 0)
        {
            column.Item().Column(payments =>
            {
                payments.Spacing(4);
                payments.Item().Text("Payments received").FontSize(12).SemiBold();
                payments.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Payment");
                        header.Cell().Element(HeaderCell).Text("Method");
                        header.Cell().Element(HeaderCell).Text("Date");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                    });
                    foreach (var payment in booking.Payments)
                    {
                        table.Cell().Element(BodyCell).Text(payment.PaymentNo);
                        table.Cell().Element(BodyCell).Text(payment.Method ?? "Online");
                        table.Cell().Element(BodyCell).Text(Date(DateOnly.FromDateTime(payment.PaidAtUtc + AgencyTime.UtcOffset)));
                        table.Cell().Element(BodyCell).AlignRight().Text(Money(payment.Amount, booking.Currency));
                    }
                });
            });
        }

        if (booking.Status == BookingStatus.Cancelled)
            column.Item().Text("This booking was cancelled. Any refund is sent separately under our cancellation policy.").FontColor(Muted);
    }

    // ---------- Shared pieces ----------

    private static void TripBlock(IContainer container, BookingDocumentData booking) =>
        container.Column(column =>
        {
            column.Spacing(2);
            column.Item().Text(booking.TripTitle).FontSize(13).SemiBold();
            column.Item().Text(BookingType(booking.BookingType)).FontColor(Muted);
            var (from, to) = booking.BookingType == Domain.Enums.BookingType.FlexibleStay ? ("Check-in", "Check-out") : ("Starts", "Ends");
            column.Item().Text($"{from}: {Date(booking.StartDate)}");
            column.Item().Text($"{to}: {Date(booking.EndDate)}");
            column.Item().Text(booking.Nights == 0 ? "Day trip" : booking.Nights == 1 ? "1 night" : $"{booking.Nights} nights");
        });

    private static void ContactBlock(IContainer container, string heading, BookingDocumentData booking) =>
        container.Column(column =>
        {
            column.Spacing(2);
            column.Item().Text(heading).FontColor(Muted);
            column.Item().Text(booking.ContactName).SemiBold();
            column.Item().Text(booking.ContactPhone);
            if (booking.ContactEmail is not null)
                column.Item().Text(booking.ContactEmail);
        });

    private static void TotalRow(ColumnDescriptor column, string label, string amount, bool bold) =>
        column.Item().Row(row =>
        {
            var left = row.RelativeItem().Text(label);
            var right = row.RelativeItem().AlignRight().Text(amount);
            if (bold)
            {
                left.SemiBold();
                right.SemiBold();
            }
        });

    private static IContainer HeaderCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(4).DefaultTextStyle(style => style.SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Line).PaddingVertical(4);

    private static string BookingType(BookingType type) => type switch
    {
        Domain.Enums.BookingType.FixedDeparture => "Fixed departure",
        Domain.Enums.BookingType.FlexibleStay => "Flexible stay",
        _ => "Custom trip"
    };

    private static string TravellerType(TravellerType type) => type switch
    {
        Domain.Enums.TravellerType.Adult => "Adult",
        Domain.Enums.TravellerType.Child => "Child",
        _ => "Infant"
    };

    private static string Date(DateOnly date) => date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>"BDT 34,000.00" - always two decimals on an invoice.</summary>
    private static string Money(decimal amount, string currency) =>
        $"{currency} {amount.ToString("#,0.00", CultureInfo.InvariantCulture)}";
}
