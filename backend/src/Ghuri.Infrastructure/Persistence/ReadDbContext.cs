using Ghuri.Application.Abstractions.Data;
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
/// IReadDbContext on top of the same AppDbContext (blueprint section 8.2),
/// with every set wrapped in AsNoTracking. One database, one connection
/// setup - just a read-only "view" of it for query handlers.
/// </summary>
internal sealed class ReadDbContext(AppDbContext db) : IReadDbContext
{
    public IQueryable<User> Users => db.Users.AsNoTracking();
    public IQueryable<Role> Roles => db.Roles.AsNoTracking();
    public IQueryable<UserRole> UserRoles => db.UserRoles.AsNoTracking();

    public IQueryable<Country> Countries => db.Countries.AsNoTracking();
    public IQueryable<Destination> Destinations => db.Destinations.AsNoTracking();
    public IQueryable<Category> Categories => db.Categories.AsNoTracking();
    public IQueryable<TourPackage> TourPackages => db.TourPackages.AsNoTracking();
    public IQueryable<PackageCategory> PackageCategories => db.PackageCategories.AsNoTracking();
    public IQueryable<PackageImage> PackageImages => db.PackageImages.AsNoTracking();
    public IQueryable<ItineraryDay> ItineraryDays => db.ItineraryDays.AsNoTracking();
    public IQueryable<PackageAddOn> PackageAddOns => db.PackageAddOns.AsNoTracking();
    public IQueryable<Departure> Departures => db.Departures.AsNoTracking();

    public IQueryable<Booking> Bookings => db.Bookings.AsNoTracking();
    public IQueryable<BookingTraveller> BookingTravellers => db.BookingTravellers.AsNoTracking();
    public IQueryable<BookingAddOn> BookingAddOns => db.BookingAddOns.AsNoTracking();
    public IQueryable<BookingStatusHistory> BookingStatusHistory => db.BookingStatusHistory.AsNoTracking();
    public IQueryable<CancellationPolicy> CancellationPolicies => db.CancellationPolicies.AsNoTracking();

    public IQueryable<Payment> Payments => db.Payments.AsNoTracking();
    public IQueryable<PaymentEvent> PaymentEvents => db.PaymentEvents.AsNoTracking();
    public IQueryable<Refund> Refunds => db.Refunds.AsNoTracking();

    public IQueryable<Coupon> Coupons => db.Coupons.AsNoTracking();
    public IQueryable<CouponRedemption> CouponRedemptions => db.CouponRedemptions.AsNoTracking();
    public IQueryable<Review> Reviews => db.Reviews.AsNoTracking();
    public IQueryable<Wishlist> Wishlists => db.Wishlists.AsNoTracking();

    public IQueryable<Banner> Banners => db.Banners.AsNoTracking();
    public IQueryable<Page> Pages => db.Pages.AsNoTracking();
    public IQueryable<BlogPost> BlogPosts => db.BlogPosts.AsNoTracking();
    public IQueryable<MenuItem> MenuItems => db.MenuItems.AsNoTracking();

    public IQueryable<CustomTourRequest> CustomTourRequests => db.CustomTourRequests.AsNoTracking();
    public IQueryable<ContactMessage> ContactMessages => db.ContactMessages.AsNoTracking();

    public IQueryable<NotificationTemplate> NotificationTemplates => db.NotificationTemplates.AsNoTracking();
    public IQueryable<Notification> Notifications => db.Notifications.AsNoTracking();

    public IQueryable<FileObject> FileObjects => db.FileObjects.AsNoTracking();
    public IQueryable<AuditLog> AuditLogs => db.AuditLogs.AsNoTracking();
    public IQueryable<SystemSetting> SystemSettings => db.SystemSettings.AsNoTracking();
}
