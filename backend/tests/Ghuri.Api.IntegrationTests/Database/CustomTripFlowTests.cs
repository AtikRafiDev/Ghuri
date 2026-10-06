using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Features.Booking.Commands.CancelMyBooking;
using Ghuri.Application.Features.Booking.Queries.GetMyBooking;
using Ghuri.Application.Features.Booking.Queries.GetMyBookingDocument;
using Ghuri.Application.Features.CustomTrips;
using Ghuri.Application.Features.CustomTrips.Commands.AcceptCustomTripQuote;
using Ghuri.Application.Features.CustomTrips.Commands.CancelCustomTrip;
using Ghuri.Application.Features.Payments.Commands.HandleGatewayCallback;
using Ghuri.Application.Features.Payments.Commands.InitiatePayment;
using Ghuri.Application.Features.CustomTrips.Commands.QuoteCustomTrip;
using Ghuri.Application.Features.CustomTrips.Commands.RejectCustomTrip;
using Ghuri.Application.Features.CustomTrips.Commands.SubmitCustomTrip;
using Ghuri.Application.Features.CustomTrips.Queries;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Ghuri.Infrastructure.Jobs;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Ghuri.Api.IntegrationTests.Database.BookingTestData;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// Custom trips against real SQL Server (17-day plan, Day 13 - "done when: a
/// trip can be submitted and quoted through the API"). The plan's example
/// throughout: Cox's Bazar 3 nights → Sylhet 2 nights.
/// </summary>
public class CustomTripFlowTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _sql;

    public CustomTripFlowTests(SqlServerFixture sql)
    {
        _sql = sql;
        _sql.Emails.Reset();
        _sql.PaymentGateway.Reset();
    }

    private OutboxDispatcherJob Outbox => _sql.Services.GetRequiredService<OutboxDispatcherJob>();

    private async Task<(Guid CoxsBazar, Guid Sylhet)> DestinationsAsync()
    {
        var coxs = Destination.Create(_sql.CountryId, "Cox's Bazar " + Unique(), Slug.Create("coxs-" + Unique()));
        var sylhet = Destination.Create(_sql.CountryId, "Sylhet " + Unique(), Slug.Create("sylhet-" + Unique()));
        await _sql.SaveAsync(coxs, sylhet);
        return (coxs.Id, sylhet.Id);
    }

    /// <summary>A customer WITH an email - the quote goes there.</summary>
    private async Task<User> CustomerAsync()
    {
        var phone = PhoneNumber.Create("016" + Random.Shared.Next(0, 100_000_000).ToString("D8"));
        var user = User.Create("Karim Hasan", phone, $"karim-{Unique()}@example.com", null);
        user.AssignRole(SystemRole.Customer, DateTime.UtcNow);
        await _sql.SaveAsync(user);
        return user;
    }

    private static SubmitCustomTripCommand Request(Guid coxs, Guid sylhet, DateOnly? start = null) =>
        new(start ?? Today.AddDays(30), Adults: 2, Children: 1, Infants: 0, HotelLevel.Standard, BudgetPerPerson: 20_000,
            Notes: "Sea-facing room please",
            [new SubmitCustomTripLeg(coxs, 3, TransferMode.Air), new SubmitCustomTripLeg(sylhet, 2, TransferMode.None)]);

    private static QuoteCustomTripCommand Quote(string tripNo, decimal hotel = 38_000) =>
        new(tripNo, "Day 1: fly to Cox's Bazar…\nDay 4: fly to Sylhet…",
            [new QuoteCustomTripLine(QuoteLineCategory.Hotel, "Sea Pearl 3N + Rose View 2N", hotel),
             new QuoteCustomTripLine(QuoteLineCategory.Transport, "Flights", 24_000)],
            ValidDays: null);

    private async Task<(User Customer, string TripNo)> SubmittedAsync()
    {
        var (coxs, sylhet) = await DestinationsAsync();
        var customer = await CustomerAsync();
        var result = await _sql.SendCommandAsync(Request(coxs, sylhet), customer.Id);
        Assert.True(result.IsSuccess, result.Error.Message);
        return (customer, result.Value.TripNo);
    }

    private List<EmailMessage> EmailsAbout(string tripNo) => _sql.Emails.Sent.Where(e => e.Subject.Contains(tripNo)).ToList();

    // ---------- Submitting ----------

    [Fact]
    public async Task Submitting_SavesTheStopsWithTheirDates_AndEmailsStaffAndTheCustomer()
    {
        var (coxs, sylhet) = await DestinationsAsync();
        var customer = await CustomerAsync();
        var start = Today.AddDays(30);

        var result = await _sql.SendCommandAsync(Request(coxs, sylhet, start), customer.Id);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.StartsWith("CT", result.Value.TripNo);
        Assert.Equal((start.AddDays(5), 5), (result.Value.EndDate, result.Value.TotalNights));

        var trip = (await _sql.SendAsync(new GetMyCustomTripQuery(result.Value.TripNo), customer.Id)).Value;
        Assert.Equal(CustomTripStatus.Submitted, trip.Status);
        Assert.Equal(
            new[] { (start, start.AddDays(3), TransferMode.Air), (start.AddDays(3), start.AddDays(5), TransferMode.None) },
            trip.Legs.Select(l => (l.CheckInDate, l.CheckOutDate, l.TransferToNext)));
        Assert.Equal((customer.FullName, customer.Email), (trip.ContactName, trip.ContactEmail)); // the contact is the account
        Assert.Null(trip.Quote);

        await Outbox.RunOnceAsync(TestContext.Current.CancellationToken);
        var emails = EmailsAbout(result.Value.TripNo);
        Assert.Contains(emails, e => e.To == "bookings@ghuri.local" && e.Subject.StartsWith("New custom trip request")); // Agency:BookingsEmail
        Assert.Contains(emails, e => e.To == customer.Email && e.HtmlBody.Contains("within 24 hours"));
    }

    [Fact]
    public async Task Submitting_TooSoon_OrToAnUnknownPlace_IsRefused()
    {
        var (coxs, sylhet) = await DestinationsAsync();
        var customer = await CustomerAsync();

        var tooSoon = await _sql.SendCommandAsync(Request(coxs, sylhet, Today.AddDays(1)), customer.Id);
        var unknown = await _sql.SendCommandAsync(Request(coxs, Guid.NewGuid()), customer.Id);
        var noStops = await _sql.SendCommandAsync(Request(coxs, sylhet) with { Legs = [] }, customer.Id);

        Assert.Equal(("trip_start_too_soon", "destination_not_found", "validation_failed"), (tooSoon.Error.Code, unknown.Error.Code, noStops.Error.Code));
    }

    // ---------- Quoting ----------

    [Fact]
    public async Task Quoting_SendsThePrice_AndQueuesAnSms()
    {
        var (customer, tripNo) = await SubmittedAsync();
        var staff = await _sql.NewCustomerAsync();
        await Outbox.RunOnceAsync(TestContext.Current.CancellationToken); // the "submitted" emails
        _sql.Emails.Reset();

        var result = await _sql.SendCommandAsync(Quote(tripNo), staff);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal((1, 62_000m), (result.Value.QuoteVersion, result.Value.Total));

        var trip = (await _sql.SendAsync(new GetMyCustomTripQuery(tripNo), customer.Id)).Value;
        Assert.Equal((CustomTripStatus.Quoted, 62_000m, false), (trip.Status, trip.Quote!.Total, trip.Quote.IsExpired));
        Assert.Equal(2, trip.Quote.Lines.Count);

        await Outbox.RunOnceAsync(TestContext.Current.CancellationToken);
        var email = Assert.Single(EmailsAbout(tripNo));
        Assert.Equal(customer.Email, email.To);
        Assert.Contains("৳62,000", email.Subject);
        Assert.Contains($"http://localhost:5173/account/trips/{tripNo}", email.HtmlBody);

        await using var scope = _sql.Services.CreateAsyncScope();
        var sms = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Notifications.AsNoTracking()
            .SingleAsync(n => n.UserId == customer.Id && n.TemplateCode == "CUSTOM_TRIP_QUOTED");
        Assert.Equal((NotificationChannel.Sms, NotificationStatus.Queued, customer.PhoneNumber.Value), (sms.Channel, sms.Status, sms.Destination));
        Assert.Contains("Tk 62,000", sms.Body);
        Assert.True(sms.Body.Length <= 160 + 60, $"SMS too long: {sms.Body.Length}"); // the link makes it 2 SMS at most
    }

    [Fact]
    public async Task TwoQuotesBeforeTheEmailGoesOut_OnlyTheLatestIsSent()
    {
        var (_, tripNo) = await SubmittedAsync();
        var staff = await _sql.NewCustomerAsync();
        await Outbox.RunOnceAsync(TestContext.Current.CancellationToken);
        _sql.Emails.Reset();

        await _sql.SendCommandAsync(Quote(tripNo, hotel: 38_000), staff);
        var second = await _sql.SendCommandAsync(Quote(tripNo, hotel: 30_000), staff); // the customer phoned: cheaper hotels

        Assert.Equal(2, second.Value.QuoteVersion);
        await Outbox.RunOnceAsync(TestContext.Current.CancellationToken);
        var email = Assert.Single(EmailsAbout(tripNo)); // not two contradicting prices
        Assert.Contains("৳54,000", email.Subject);
        Assert.StartsWith("Updated quote", email.Subject);
    }

    [Fact]
    public async Task Rejecting_TellsTheCustomerWhy_AndEndsIt()
    {
        var (customer, tripNo) = await SubmittedAsync();
        await Outbox.RunOnceAsync(TestContext.Current.CancellationToken);
        _sql.Emails.Reset();

        var result = await _sql.SendCommandAsync(new RejectCustomTripCommand(tripNo, "No flights to Sylhet that week"));

        Assert.True(result.IsSuccess, result.Error.Message);
        await Outbox.RunOnceAsync(TestContext.Current.CancellationToken);
        Assert.Contains("No flights to Sylhet that week", Assert.Single(EmailsAbout(tripNo)).HtmlBody);
        Assert.Equal("trip_not_quotable", (await _sql.SendCommandAsync(Quote(tripNo), await _sql.NewCustomerAsync())).Error.Code);
        Assert.Equal("trip_not_cancellable", (await _sql.SendCommandAsync(new CancelCustomTripCommand(tripNo, null), customer.Id)).Error.Code);
    }

    // ---------- Cancelling ----------

    [Fact]
    public async Task TheCustomer_CanCancelTheirOwn_ButNotSomeoneElses()
    {
        var (customer, tripNo) = await SubmittedAsync();

        var stranger = await _sql.SendCommandAsync(new CancelCustomTripCommand(tripNo, null), await _sql.NewCustomerAsync());
        var own = await _sql.SendCommandAsync(new CancelCustomTripCommand(tripNo, "Plans changed"), customer.Id);

        Assert.Equal("custom_trip_not_found", stranger.Error.Code);
        Assert.True(own.IsSuccess, own.Error.Message);
        var trip = (await _sql.SendAsync(new GetMyCustomTripQuery(tripNo), customer.Id)).Value;
        Assert.Equal((CustomTripStatus.Cancelled, "Plans changed", false), (trip.Status, trip.Timeline.CancelReason, trip.CanCancel));
    }

    // ---------- Expiry ----------

    [Fact]
    public async Task TheExpiryJob_ExpiresOnlyOverdueQuotes_AndStaffCanQuoteAgain()
    {
        var (customer, tripNo) = await SubmittedAsync();
        var staff = await _sql.NewCustomerAsync();
        await _sql.SendCommandAsync(Quote(tripNo), staff);
        var job = _sql.Services.GetRequiredService<QuoteExpiryJob>();

        await job.RunOnceAsync(TestContext.Current.CancellationToken);
        Assert.Equal(CustomTripStatus.Quoted, (await _sql.SendAsync(new GetMyCustomTripQuery(tripNo), customer.Id)).Value.Status); // still valid

        await using (var scope = _sql.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().CustomTrips
                .Where(t => t.TripNo == tripNo)
                .ExecuteUpdateAsync(set => set.SetProperty(t => t.QuoteExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));
        }
        await job.RunOnceAsync(TestContext.Current.CancellationToken);
        await job.RunOnceAsync(TestContext.Current.CancellationToken); // twice changes nothing

        var expired = (await _sql.SendAsync(new GetMyCustomTripQuery(tripNo), customer.Id)).Value;
        Assert.Equal((CustomTripStatus.Expired, true), (expired.Status, expired.Quote!.IsExpired));
        Assert.NotNull(expired.Timeline.ExpiredAtUtc);

        var again = await _sql.SendCommandAsync(Quote(tripNo), staff);
        Assert.Equal((true, 2), (again.IsSuccess, again.Value.QuoteVersion));
    }

    // ---------- Lists ----------

    [Fact]
    public async Task MyTrips_ShowTheRoute_AndOnlyMine()
    {
        var (customer, tripNo) = await SubmittedAsync();
        await SubmittedAsync(); // someone else's

        var mine = (await _sql.SendAsync(new GetMyCustomTripsQuery(), customer.Id)).Value;

        var only = Assert.Single(mine);
        Assert.Equal(tripNo, only.TripNo);
        Assert.Matches(@"^Cox's Bazar .+ → Sylhet .+$", only.Route);
        Assert.Equal(3, only.People);
    }

    [Fact]
    public async Task TheStaffQueue_ShowsWaitingRequests_OldestFirst()
    {
        var (_, first) = await SubmittedAsync();
        var (_, second) = await SubmittedAsync();

        var queue = (await _sql.SendAsync(new GetCustomTripQueueQuery(CustomTripStatus.Submitted, null, 1, 100))).Value;

        var order = queue.Items.Select(t => t.TripNo).Where(n => n == first || n == second).ToList();
        Assert.Equal(new[] { first, second }, order);
    }

    [Fact]
    public async Task TheStaffView_SaysWhoQuoted()
    {
        var (_, tripNo) = await SubmittedAsync();
        var staff = await _sql.NewCustomerUserAsync();
        await _sql.SendCommandAsync(Quote(tripNo), staff.Id);

        var view = (await _sql.SendAsync(new GetCustomTripForAdminQuery(tripNo))).Value;

        Assert.Equal((staff.FullName, CustomTripStatus.Quoted), (view.QuotedByName, view.Trip.Status));
    }

    // ---------- Who may quote (HTTP, real tokens) ----------

    [Theory]
    [InlineData(SystemRole.Sales, HttpStatusCode.OK)]
    [InlineData(SystemRole.Manager, HttpStatusCode.OK)]
    [InlineData(SystemRole.Accounts, HttpStatusCode.Forbidden)]
    [InlineData(SystemRole.Customer, HttpStatusCode.Forbidden)]
    public async Task OnlyCustomerFacingStaff_CanQuote(SystemRole role, HttpStatusCode expected)
    {
        var (_, tripNo) = await SubmittedAsync();
        var phone = PhoneNumber.Create("015" + Random.Shared.Next(0, 100_000_000).ToString("D8"));
        var user = User.Create($"{role} person", phone, $"{Unique()}@ghuri.local", null);
        user.AssignRole(role, DateTime.UtcNow);
        await _sql.SaveAsync(user);
        var client = _sql.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", _sql.Services.GetRequiredService<ITokenService>().CreateAccessToken(user).Value);

        var response = await client.PostAsJsonAsync($"/api/v1/admin/custom-trips/{tripNo}/quote", new
        {
            itinerary = "Day 1…",
            lines = new[] { new { category = 1, description = "Hotels", amount = 40_000 } },
            validDays = 3
        }, TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
    }

    // ---------- Day 15: accept, pay, voucher ----------

    /// <summary>A trip for 2 adults + 1 child (see Request), quoted at ৳62,000.</summary>
    private async Task<(User Customer, string TripNo)> QuotedAsync()
    {
        var (customer, tripNo) = await SubmittedAsync();
        var quoted = await _sql.SendCommandAsync(Quote(tripNo), await _sql.NewCustomerAsync());
        Assert.True(quoted.IsSuccess, quoted.Error.Message);
        return (customer, tripNo);
    }

    private static AcceptCustomTripQuoteCommand Accept(string tripNo, params AcceptTraveller[] travellers) =>
        new(tripNo,
            travellers.Length > 0
                ? travellers
                : [new(TravellerType.Adult, "Karim Hasan", IsLead: true), new(TravellerType.Adult, "Nadia Hasan", IsLead: false), new(TravellerType.Child, "Rafi", IsLead: false)],
            SpecialRequest: null);

    private async Task<CustomTripDto> TripAsync(User customer, string tripNo) =>
        (await _sql.SendAsync(new GetMyCustomTripQuery(tripNo), customer.Id)).Value;

    /// <summary>Paid the way any booking is: InitiatePayment, then SSLCommerz's IPN (the fake validates it).</summary>
    private async Task PayAsync(User customer, string bookingNo, decimal amount)
    {
        var paymentNo = (await _sql.SendCommandAsync(new InitiatePaymentCommand(bookingNo), customer.Id)).Value.PaymentNo;
        _sql.PaymentGateway.ValidatesAs(paymentNo, amount);
        var ipn = await _sql.SendCommandAsync(new HandleGatewayCallbackCommand(
            PaymentEventType.Ipn, new Dictionary<string, string> { ["tran_id"] = paymentNo, ["val_id"] = "v-" + paymentNo }));
        Assert.True(ipn.IsSuccess, ipn.Error.Message);
    }

    [Fact]
    public async Task AcceptingTheQuote_MakesABookingForExactlyThePrice_AndAClickTwiceGivesTheSameOne()
    {
        var (customer, tripNo) = await QuotedAsync();

        var first = await _sql.SendCommandAsync(Accept(tripNo), customer.Id);
        var again = await _sql.SendCommandAsync(Accept(tripNo), customer.Id);

        Assert.True(first.IsSuccess, first.Error.Message);
        Assert.Equal((62_000m, first.Value.BookingNo), (first.Value.TotalAmount, again.Value.BookingNo)); // one booking, not two
        var trip = await TripAsync(customer, tripNo);
        Assert.Equal((CustomTripStatus.Accepted, first.Value.BookingNo, BookingStatus.PendingPayment),
            (trip.Status, trip.Booking!.BookingNo, trip.Booking.Status));

        var booking = (await _sql.SendAsync(new GetMyBookingQuery(first.Value.BookingNo), customer.Id)).Value;
        Assert.Equal((BookingType.CustomTrip, $"Custom trip {tripNo}", 3), (booking.BookingType, booking.PackageTitle, booking.Travellers.Count));
        Assert.Equal((trip.StartDate, trip.EndDate), (booking.StartDate, booking.EndDate));
    }

    [Fact]
    public async Task AQuotePastItsDeadline_CantBeAccepted()
    {
        var (customer, tripNo) = await QuotedAsync();
        await using (var scope = _sql.Services.CreateAsyncScope())
        {
            // Still "Quoted" - the hourly job hasn't run - but past the deadline.
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().CustomTrips
                .Where(t => t.TripNo == tripNo)
                .ExecuteUpdateAsync(set => set.SetProperty(t => t.QuoteExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));
        }

        var result = await _sql.SendCommandAsync(Accept(tripNo), customer.Id);

        Assert.Equal("quote_expired", result.Error.Code);
        Assert.Null((await TripAsync(customer, tripNo)).Booking);
    }

    [Fact]
    public async Task OtherTravellersThanQuoted_OrSomeoneElse_CantAccept()
    {
        var (customer, tripNo) = await QuotedAsync();

        var wrongPeople = await _sql.SendCommandAsync(Accept(tripNo, new AcceptTraveller(TravellerType.Adult, "Solo", true)), customer.Id);
        var stranger = await _sql.SendCommandAsync(Accept(tripNo), await _sql.NewCustomerAsync());

        Assert.Equal(("travellers_mismatch", "custom_trip_not_found"), (wrongPeople.Error.Code, stranger.Error.Code));
        Assert.Equal(CustomTripStatus.Quoted, (await TripAsync(customer, tripNo)).Status);
    }

    [Fact]
    public async Task AnUnpaidAcceptance_Expires_AndTheQuoteCanBeAcceptedAgain()
    {
        var (customer, tripNo) = await QuotedAsync();
        var first = (await _sql.SendCommandAsync(Accept(tripNo), customer.Id)).Value;
        await EndTheHoldAsync(_sql, first.BookingNo);

        await _sql.Services.GetRequiredService<BookingExpiryJob>().RunOnceAsync(TestContext.Current.CancellationToken);

        var released = await TripAsync(customer, tripNo);
        Assert.Equal(CustomTripStatus.Quoted, released.Status); // the offer still stands
        Assert.Null(released.Booking);
        var second = await _sql.SendCommandAsync(Accept(tripNo), customer.Id);
        Assert.True(second.IsSuccess, second.Error.Message);
        Assert.NotEqual(first.BookingNo, second.Value.BookingNo);
    }

    [Fact]
    public async Task AcceptingAgain_RightAfterTheHoldRanOut_DoesntWaitForTheJob()
    {
        var (customer, tripNo) = await QuotedAsync();
        var first = (await _sql.SendCommandAsync(Accept(tripNo), customer.Id)).Value;
        await EndTheHoldAsync(_sql, first.BookingNo); // and the expiry job hasn't run

        var second = await _sql.SendCommandAsync(Accept(tripNo), customer.Id);

        Assert.True(second.IsSuccess, second.Error.Message);
        Assert.NotEqual(first.BookingNo, second.Value.BookingNo);
        Assert.Equal(BookingStatus.Expired, (await _sql.SendAsync(new GetMyBookingQuery(first.BookingNo), customer.Id)).Value.Status);
    }

    [Fact]
    public async Task EndToEnd_SubmitQuoteAcceptPay_TheTripIsPaid_AndTheVoucherIsEmailed()
    {
        var (customer, tripNo) = await QuotedAsync();
        var booking = (await _sql.SendCommandAsync(Accept(tripNo), customer.Id)).Value;
        await PayAsync(customer, booking.BookingNo, 62_000m);
        _sql.Emails.Reset();

        await Outbox.RunOnceAsync(TestContext.Current.CancellationToken);

        var trip = await TripAsync(customer, tripNo);
        Assert.Equal((CustomTripStatus.Paid, BookingStatus.Confirmed), (trip.Status, trip.Booking!.Status));
        Assert.NotNull(trip.Timeline.PaidAtUtc);

        var email = Assert.Single(_sql.Emails.Sent, e => e.Subject.Contains(booking.BookingNo));
        Assert.Equal(customer.Email, email.To);
        Assert.Contains($"Custom trip {tripNo}", email.Subject);
        Assert.Equal(new[] { $"Voucher-{booking.BookingNo}.pdf", $"Invoice-{booking.BookingNo}.pdf" }, email.Attachments!.Select(a => a.FileName));

        var voucher = await _sql.SendAsync(new GetMyBookingDocumentQuery(booking.BookingNo, BookingDocumentKind.Voucher), customer.Id);
        Assert.True(voucher.IsSuccess, voucher.Error.Message);
    }

    [Fact]
    public async Task CancellingThePaidBooking_CancelsTheTrip_AndRefundsByThePolicy()
    {
        await using (var scope = _sql.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!await db.CancellationPolicies.AnyAsync(p => p.PackageId == null))
            {
                db.CancellationPolicies.AddRange(
                    Domain.Entities.Booking.CancellationPolicy.Create(null, 30, 100), Domain.Entities.Booking.CancellationPolicy.Create(null, 0, 0));
                await db.SaveChangesAsync();
            }
        }
        var (customer, tripNo) = await QuotedAsync(); // starts in 30 days → 100% back
        var booking = (await _sql.SendCommandAsync(Accept(tripNo), customer.Id)).Value;
        await PayAsync(customer, booking.BookingNo, 62_000m);
        await Outbox.RunOnceAsync(TestContext.Current.CancellationToken);

        var cancelled = await _sql.SendCommandAsync(new CancelMyBookingCommand(booking.BookingNo, "Family emergency"), customer.Id);

        Assert.True(cancelled.IsSuccess, cancelled.Error.Message);
        Assert.Equal(62_000m, cancelled.Value.RefundAmount);
        var trip = await TripAsync(customer, tripNo);
        Assert.Equal(CustomTripStatus.Cancelled, trip.Status);
        Assert.Contains("Family emergency", trip.Timeline.CancelReason);
    }
}
