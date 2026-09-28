using Ghuri.Domain.Entities.Booking;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Cms;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Entities.Marketing;
using Ghuri.Domain.Entities.Notify;
using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.Entities.Payment;
using Ghuri.Domain.Entities.Support;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence;

/// <summary>
/// The write-side database context. Command handlers change data through
/// repositories, which use this context underneath - nothing outside
/// Infrastructure ever references AppDbContext directly.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Country> Countries => Set<Country>();

    // iam schema
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    // catalog schema
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<TourPackage> TourPackages => Set<TourPackage>();
    public DbSet<PackageCategory> PackageCategories => Set<PackageCategory>();
    public DbSet<PackageImage> PackageImages => Set<PackageImage>();
    public DbSet<ItineraryDay> ItineraryDays => Set<ItineraryDay>();
    public DbSet<PackageAddOn> PackageAddOns => Set<PackageAddOn>();
    public DbSet<Departure> Departures => Set<Departure>();

    // booking schema
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingTraveller> BookingTravellers => Set<BookingTraveller>();
    public DbSet<BookingAddOn> BookingAddOns => Set<BookingAddOn>();
    public DbSet<BookingStatusHistory> BookingStatusHistory => Set<BookingStatusHistory>();
    public DbSet<CancellationPolicy> CancellationPolicies => Set<CancellationPolicy>();

    // payment schema
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();
    public DbSet<Refund> Refunds => Set<Refund>();

    // marketing schema
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();

    // cms schema
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    // support schema
    public DbSet<CustomTourRequest> CustomTourRequests => Set<CustomTourRequest>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    // notify schema
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // ops schema
    public DbSet<FileObject> FileObjects => Set<FileObject>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    /// <summary>
    /// The blueprint's rule (section 5.1): "human-readable numbers (PKG1001
    /// package...) come from SQL SEQUENCE objects" - a real database object
    /// that hands out 1001, 1002, 1003... one at a time, safely, even if
    /// two people create a package at the exact same moment. The repository
    /// (built on Day 4) reads the next value and formats it as "PKG1001".
    /// </summary>
    public const string PackageCodeSequenceName = "PackageCodeSequence";

    /// <summary>Same idea as PackageCodeSequenceName, for booking.Bookings.BookingNo (TB100001).</summary>
    public const string BookingNoSequenceName = "BookingNoSequence";

    /// <summary>Same idea again, for payment.Payments.PaymentNo (PAY100001).</summary>
    public const string PaymentNoSequenceName = "PaymentNoSequence";

    /// <summary>Same idea again, for payment.Refunds.RefundNo (RF1001).</summary>
    public const string RefundNoSequenceName = "RefundNoSequence";

    /// <summary>Same idea again, for support.CustomTourRequests.RequestNo (CR1001).</summary>
    public const string CustomTourRequestNoSequenceName = "CustomTourRequestNoSequence";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasSequence<int>(PackageCodeSequenceName, schema: "catalog")
            .StartsAt(1001)
            .IncrementsBy(1);

        modelBuilder.HasSequence<int>(BookingNoSequenceName, schema: "booking")
            .StartsAt(100001)
            .IncrementsBy(1);

        modelBuilder.HasSequence<int>(PaymentNoSequenceName, schema: "payment")
            .StartsAt(100001)
            .IncrementsBy(1);

        modelBuilder.HasSequence<int>(RefundNoSequenceName, schema: "payment")
            .StartsAt(1001)
            .IncrementsBy(1);

        modelBuilder.HasSequence<int>(CustomTourRequestNoSequenceName, schema: "support")
            .StartsAt(1001)
            .IncrementsBy(1);

        // Finds every class in this project implementing
        // IEntityTypeConfiguration<T> (like CountryConfiguration) and applies
        // it automatically. This means adding a new entity later never
        // requires touching this file - just add its Configuration class.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
