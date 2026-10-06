using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Cms;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Entities.Marketing;
using Ghuri.Domain.Entities.Notify;
using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.Entities.Payment;
using Ghuri.Domain.Entities.Support;

namespace Ghuri.Application.Abstractions.Data;

/// <summary>
/// The READ side of CQRS (blueprint section 8.2). Query handlers use this
/// and nothing else: plain IQueryables they filter and project straight
/// into DTOs with .Select(...).
/// </summary>
/// <remarks>
/// What makes it read-only by construction:
/// - Only IQueryable - no Add, no Remove, and no SaveChanges exists here,
///   so a query handler physically can't change data.
/// - Every set is AsNoTracking (see ReadDbContext): EF Core skips the
///   change-tracking bookkeeping, so reads are faster and use less memory.
/// - Soft-deleted rows are already filtered out (global query filter).
///
/// Deliberately NOT exposed here: RefreshTokens and OtpCodes (security
/// secrets - only the auth commands may touch them), OutboxMessages and
/// IdempotencyKeys (internal plumbing - no screen ever shows them).
/// </remarks>
public interface IReadDbContext
{
    // iam
    IQueryable<User> Users { get; }
    IQueryable<Role> Roles { get; }
    IQueryable<UserRole> UserRoles { get; }

    // catalog
    IQueryable<Country> Countries { get; }
    IQueryable<Destination> Destinations { get; }
    IQueryable<DestinationImage> DestinationImages { get; }
    IQueryable<Category> Categories { get; }
    IQueryable<TourPackage> TourPackages { get; }
    IQueryable<PackageCategory> PackageCategories { get; }
    IQueryable<PackageImage> PackageImages { get; }
    IQueryable<ItineraryDay> ItineraryDays { get; }
    IQueryable<PackageAddOn> PackageAddOns { get; }
    IQueryable<Departure> Departures { get; }

    // booking
    IQueryable<Booking> Bookings { get; }
    IQueryable<BookingTraveller> BookingTravellers { get; }
    IQueryable<BookingAddOn> BookingAddOns { get; }
    IQueryable<BookingStatusHistory> BookingStatusHistory { get; }
    IQueryable<CancellationPolicy> CancellationPolicies { get; }
    IQueryable<CustomTrip> CustomTrips { get; }
    IQueryable<CustomTripLeg> CustomTripLegs { get; }
    IQueryable<CustomTripQuoteLine> CustomTripQuoteLines { get; }

    // payment
    IQueryable<Payment> Payments { get; }
    IQueryable<PaymentEvent> PaymentEvents { get; }
    IQueryable<Refund> Refunds { get; }

    // marketing
    IQueryable<Coupon> Coupons { get; }
    IQueryable<CouponRedemption> CouponRedemptions { get; }
    IQueryable<Review> Reviews { get; }
    IQueryable<Wishlist> Wishlists { get; }

    // cms
    IQueryable<Banner> Banners { get; }
    IQueryable<Page> Pages { get; }
    IQueryable<BlogPost> BlogPosts { get; }
    IQueryable<MenuItem> MenuItems { get; }

    // support
    IQueryable<CustomTourRequest> CustomTourRequests { get; }
    IQueryable<ContactMessage> ContactMessages { get; }

    // notify
    IQueryable<NotificationTemplate> NotificationTemplates { get; }
    IQueryable<Notification> Notifications { get; }

    // ops
    IQueryable<FileObject> FileObjects { get; }
    IQueryable<AuditLog> AuditLogs { get; }
    IQueryable<SystemSetting> SystemSettings { get; }
}
