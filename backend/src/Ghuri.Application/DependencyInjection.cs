using FluentValidation;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Behaviors;
using Ghuri.Application.Features.Booking;
using Ghuri.Application.Features.Booking.Documents;
using Ghuri.Application.Features.CustomTrips;
using Ghuri.Application.Features.CustomTrips.Events;
using Ghuri.Application.Features.Identity;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Ghuri.Application;

/// <summary>
/// Registers everything the Application layer provides. Program.cs calls
/// this one method - it never needs to know Mediator or FluentValidation
/// exist (blueprint: "each layer registers itself via AddApplication /
/// AddInfrastructure").
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AddMediator is not in any library - Mediator's source generator
        // WRITES it at compile time, containing one registration per
        // handler it finds. No reflection scanning at startup.
        services.AddMediator((MediatorOptions options) =>
        {
            // Scoped = one instance per HTTP request. Handlers will use
            // repositories -> AppDbContext, which is scoped; a Singleton
            // handler holding a scoped DbContext would share one context
            // across every request at once (the library's default is
            // Singleton, so this line matters).
            options.ServiceLifetime = ServiceLifetime.Scoped;

            // ORDER MATTERS - first in the list is the OUTERMOST wrapper:
            //   Validation -> Logging -> Transaction -> Handler
            // Invalid input is rejected before anything is logged or any
            // transaction opens (blueprint section 7.2, pipeline order).
            options.PipelineBehaviors =
            [
                typeof(ValidationBehavior<,>),
                typeof(LoggingBehavior<,>),
                typeof(TransactionBehavior<,>)
            ];
        });

        // Finds every AbstractValidator<T> in this project and registers
        // it - adding a validator for a new command needs no extra wiring.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddIdentityFeature();
        services.AddDomainEventHandlers();
        services.AddScoped<CancellationTerms>();
        services.AddScoped<BookingDocumentLoader>();
        services.AddCustomTripsFeature();

        return services;
    }

    private static void AddCustomTripsFeature(this IServiceCollection services)
    {
        // Links in emails must be full addresses - checked at startup, not when the first quote goes out.
        services.AddOptions<SiteOptions>()
            .BindConfiguration(SiteOptions.SectionName)
            .Validate(o => Uri.TryCreate(o.PublicUrl, UriKind.Absolute, out _),
                "Site:PublicUrl must be the website's full address, e.g. http://localhost:5173")
            .ValidateOnStart();
        services.AddOptions<StaffAlertOptions>().BindConfiguration(StaffAlertOptions.SectionName);

        services.AddScoped<CustomTripReader>();
        services.AddScoped<CustomTripMessageFormat>();
        services.AddScoped<CustomTripBookingSync>();
    }

    /// <summary>
    /// Finds every IDomainEventHandler&lt;T&gt; in this project and registers it
    /// (scoped - handlers read the database). Like the validators: a new
    /// handler needs no extra wiring.
    /// </summary>
    private static void AddDomainEventHandlers(this IServiceCollection services)
    {
        var handlers = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>))
                .Select(i => (Service: i, Implementation: type)));

        foreach (var (service, implementation) in handlers)
            services.AddScoped(service, implementation);
    }

    private static void AddIdentityFeature(this IServiceCollection services)
    {
        // The login numbers from the "Auth" section of appsettings.json,
        // checked when the app STARTS - a typo or a missing value stops it
        // immediately, instead of breaking the first login hours later.
        services.AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName)
            .Validate(o => o.MaxFailedLogins is >= 1 and <= byte.MaxValue,
                "Auth:MaxFailedLogins must be between 1 and 255.")
            .Validate(o => o.LockoutMinutes > 0 && o.RefreshTokenDays > 0
                           && o.PasswordResetLinkMinutes > 0 && o.MaxResetEmailsPerHour > 0,
                "Auth: LockoutMinutes, RefreshTokenDays, PasswordResetLinkMinutes and MaxResetEmailsPerHour must all be greater than 0.")
            .Validate(o => Uri.TryCreate(o.PasswordResetUrl, UriKind.Absolute, out _),
                "Auth:PasswordResetUrl must be a full address, e.g. http://localhost:5173/reset-password")
            .ValidateOnStart();

        // Scoped: it uses repositories, which share the request's DbContext.
        services.AddScoped<SessionIssuer>();
    }
}
