using System.Text;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Repositories;
using Ghuri.Infrastructure.Documents;
using Ghuri.Infrastructure.Images;
using Ghuri.Infrastructure.Jobs;
using Ghuri.Infrastructure.Payments;
using Ghuri.Infrastructure.Messaging;
using Ghuri.Infrastructure.Persistence;
using Ghuri.Infrastructure.Persistence.Interceptors;
using Ghuri.Infrastructure.Persistence.Repositories;
using Ghuri.Infrastructure.Persistence.Seed;
using Ghuri.Infrastructure.Security;
using Ghuri.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ghuri.Infrastructure;

/// <summary>
/// Registers everything Infrastructure provides into the app's DI
/// container. Program.cs calls this ONE method instead of knowing about
/// AppDbContext, connection strings, or EF Core directly - that knowledge
/// stays inside Infrastructure, where it belongs.
/// </summary>
/// <remarks>
/// Expects the caller (the Api) to register ICurrentUser - "who is the
/// current user" depends on HTTP, which Infrastructure doesn't know about.
/// If that registration is ever missing, ASP.NET Core's startup check
/// fails immediately in Development instead of at the first save.
/// </remarks>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // The real clock. "Try" = only if nobody registered one already, so
        // a test can register a fixed fake clock first and keep it.
        services.TryAddSingleton(TimeProvider.System);

        services.AddPersistence(configuration);
        services.AddSecurity();
        services.AddEmail(configuration);
        services.AddDocuments();
        services.AddFileStorage();
        services.AddBackgroundJobs();
        services.AddPaymentGateway();

        return services;
    }

    private static void AddPaymentGateway(this IServiceCollection services)
    {
        services.AddOptions<SslCommerzOptions>()
            .BindConfiguration(SslCommerzOptions.SectionName)
            .Validate(o => !string.IsNullOrWhiteSpace(o.StoreId) && !string.IsNullOrWhiteSpace(o.StorePassword),
                "PaymentGateway:SslCommerz:StoreId and StorePassword are required (sandbox: appsettings.Development.json; " +
                "a server: environment variables PaymentGateway__SslCommerz__StoreId / __StorePassword).")
            .Validate(o => Uri.TryCreate(o.CallbackBaseUrl, UriKind.Absolute, out _),
                "PaymentGateway:SslCommerz:CallbackBaseUrl must be the site's full address, e.g. http://localhost:5173")
            .Validate(o => string.IsNullOrWhiteSpace(o.IpnUrl) || Uri.TryCreate(o.IpnUrl, UriKind.Absolute, out _),
                "PaymentGateway:SslCommerz:IpnUrl must be empty or a full address.")
            .Validate(o => o.TimeoutSeconds is >= 5 and <= 120,
                "PaymentGateway:SslCommerz:TimeoutSeconds must be between 5 and 120.")
            .ValidateOnStart();

        // ONE long-lived HttpClient for the gateway - Microsoft's guidance when
        // not using IHttpClientFactory: reusing it avoids running out of
        // sockets, and PooledConnectionLifetime renews connections so a DNS
        // change at SSLCommerz is picked up. (IHttpClientFactory would need
        // another package, and logs every request address by default - and
        // the Day 10 validation call has the store password in its address.)
        services.AddSingleton<IPaymentGateway>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<SslCommerzOptions>>();
            var http = new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
            {
                Timeout = TimeSpan.FromSeconds(settings.Value.TimeoutSeconds)
            };
            return new SslCommerzGateway(http, settings, sp.GetRequiredService<ILogger<SslCommerzGateway>>());
        });
    }

    private static void AddBackgroundJobs(this IServiceCollection services)
    {
        services.AddOptions<BookingExpiryOptions>()
            .BindConfiguration(BookingExpiryOptions.SectionName)
            .Validate(o => o.IntervalSeconds is >= 5 and <= 3600,
                "Jobs:BookingExpiry:IntervalSeconds must be between 5 and 3600.")
            .Validate(o => o.BatchSize is >= 1 and <= 1000,
                "Jobs:BookingExpiry:BatchSize must be between 1 and 1000.")
            .ValidateOnStart();

        // One instance, registered twice: as itself (so the integration tests
        // can call RunOnceAsync directly) and as the hosted service the app
        // starts and stops. "dotnet run -- seed" never starts hosted services.
        services.AddSingleton<BookingExpiryJob>();
        services.AddHostedService(sp => sp.GetRequiredService<BookingExpiryJob>());

        // Sends what the domain events ask for (the voucher email) - see OutboxDispatcherJob.
        services.AddOptions<OutboxOptions>()
            .BindConfiguration(OutboxOptions.SectionName)
            .Validate(o => o.IntervalSeconds is >= 1 and <= 3600,
                "Jobs:Outbox:IntervalSeconds must be between 1 and 3600.")
            .Validate(o => o.BatchSize is >= 1 and <= 1000 && o.MaxAttempts is >= 1 and <= 20,
                "Jobs:Outbox:BatchSize must be between 1 and 1000, and MaxAttempts between 1 and 20.")
            .Validate(o => o.RetryDelaySeconds is >= 0 and <= 3600,
                "Jobs:Outbox:RetryDelaySeconds must be between 0 and 3600.")
            .ValidateOnStart();
        services.AddSingleton<OutboxDispatcherJob>();
        services.AddHostedService(sp => sp.GetRequiredService<OutboxDispatcherJob>());
    }

    private static void AddFileStorage(this IServiceCollection services)
    {
        services.AddOptions<LocalDiskStorageOptions>()
            .BindConfiguration(LocalDiskStorageOptions.SectionName)
            // Turn "uploads" into "F:\...\Ghuri.Api\uploads" once, at startup,
            // so the storage and the Api's file serving use the exact same folder.
            .PostConfigure(o =>
            {
                if (!string.IsNullOrWhiteSpace(o.RootPath))
                    o.RootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(o.RootPath));
            })
            .Validate(o => !string.IsNullOrWhiteSpace(o.RootPath),
                "Storage:RootPath is required (appsettings.json), e.g. \"uploads\".")
            .Validate(o => o.PublicBaseUrl.StartsWith('/') && !o.PublicBaseUrl.EndsWith('/'),
                "Storage:PublicBaseUrl must start with / and not end with one, e.g. \"/files\".")
            .ValidateOnStart();

        // Singletons: neither keeps per-request state.
        services.AddSingleton<IFileStorage, LocalDiskFileStorage>();
        services.AddSingleton<IImageProcessor, SkiaImageProcessor>();
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found in configuration.");

        // Scoped, not singleton: it depends on ICurrentUser, which changes
        // with every request.
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddSingleton<OutboxInterceptor>(); // keeps no state - only the clock

        // The (serviceProvider, options) overload lets each DbContext pick
        // up the interceptor instance for ITS OWN request scope.
        services.AddDbContext<AppDbContext>((serviceProvider, options) => options
            // Split queries by default: a query that loads several lists
            // (a package's photos AND days AND categories) runs one SELECT
            // per list, instead of one JOIN returning every combination of
            // them (15 photos × 10 days × 3 categories = 450 rows for one
            // package - "cartesian explosion"). Set here so Application's
            // query handlers get it without referencing SQL-specific EF.
            .UseSqlServer(connectionString, sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntityInterceptor>(),
                serviceProvider.GetRequiredService<OutboxInterceptor>())
            // When the BROWSER hangs up mid-request, EF logs the cancelled
            // transaction as an Error - noise that buries real errors.
            // Lowered to Warning. Nothing is lost: a REAL transaction
            // failure is still logged as an Error by the request log and
            // by GlobalExceptionHandler.
            .ConfigureWarnings(warnings => warnings.Log((RelationalEventId.TransactionError, LogLevel.Warning))));

        // Scoped, same as AppDbContext - the unit of work and the handler
        // must share the SAME DbContext instance within one request, or
        // SaveChanges would save a different context than the one the
        // handler changed.
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // The read side - a no-tracking view over the same AppDbContext.
        services.AddScoped<IReadDbContext, ReadDbContext>();

        // The write side - one repository per aggregate, used only by
        // command handlers. Scoped for the same reason as EfUnitOfWork:
        // they must share the request's one AppDbContext.
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IFileObjectRepository, FileObjectRepository>();
        services.AddScoped<IDestinationRepository, DestinationRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITourPackageRepository, TourPackageRepository>();
        services.AddScoped<IDepartureRepository, DepartureRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();

        // "Same request twice = same answer" for POSTs like CreateBooking. Scoped:
        // it must use the request's one AppDbContext, so it joins the transaction.
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();

        // Only ever resolved by the seed methods below ("dotnet run -- seed" / "-- seed-demo").
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<DemoPackageSeeder>();

        // "Can we actually reach SQL Server?" - tagged "ready" so it only
        // runs on /health/ready, not on the lightweight /health/live
        // (see Program.cs for why the two are different).
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(name: "database", tags: [HealthCheckTags.Ready]);
    }

    private static void AddSecurity(this IServiceCollection services)
    {
        // Fill JwtOptions from the "Jwt" section and CHECK it when the app
        // starts: a missing signing key stops the API immediately with a
        // clear message, instead of failing at the first login.
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer) && !string.IsNullOrWhiteSpace(o.Audience),
                "Jwt:Issuer and Jwt:Audience are required (appsettings.json).")
            // HMAC-SHA256 needs a key of at least 256 bits = 32 bytes.
            .Validate(o => Encoding.UTF8.GetByteCount(o.SigningKey) >= 32,
                "Jwt:SigningKey is missing or shorter than 32 characters. Locally it comes from " +
                "appsettings.Development.json; on a server, from the Jwt__SigningKey environment variable.")
            .Validate(o => o.AccessTokenMinutes is > 0 and <= 60,
                "Jwt:AccessTokenMinutes must be between 1 and 60.")
            .ValidateOnStart();

        // Singletons: neither keeps any per-request state, so one shared
        // instance for the whole app is safe and cheapest.
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
    }

    private static void AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        // Chosen by configuration, not by environment name: a staging
        // server could use a real sender or the log, without code changes.
        var sender = configuration["Email:Sender"];
        switch (sender)
        {
            case "Log":
                services.AddSingleton<IEmailSender, LogEmailSender>();
                break;
            case "Smtp":
                services.AddOptions<SmtpEmailOptions>()
                    .BindConfiguration(SmtpEmailOptions.SectionName)
                    .Validate(o => !string.IsNullOrWhiteSpace(o.FromAddress),
                        "Email:FromAddress is required, e.g. bookings@ghuri.com.")
                    .Validate(o => !string.IsNullOrWhiteSpace(o.Smtp.Host) && o.Smtp.Port is > 0 and <= 65535,
                        "Email:Smtp:Host and Email:Smtp:Port are required, e.g. localhost and 25 for smtp4dev.")
                    .ValidateOnStart();
                services.AddSingleton<IEmailSender, SmtpEmailSender>();
                break;
            default:
                throw new InvalidOperationException(
                    $"Email:Sender '{sender}' is not supported. Use \"Smtp\" (a mail server - smtp4dev locally) " +
                    "or \"Log\" (development only - writes emails to the console).");
        }
    }

    /// <summary>The invoice and e-voucher PDFs (Day 11), with the agency's details from "Agency".</summary>
    private static void AddDocuments(this IServiceCollection services)
    {
        services.AddOptions<AgencyOptions>()
            .BindConfiguration(AgencyOptions.SectionName)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Name), "Agency:Name is required - it's printed on every invoice and voucher.")
            .ValidateOnStart();
        services.AddSingleton<IBookingDocumentRenderer, QuestPdfBookingDocumentRenderer>();
    }

    /// <summary>
    /// Creates the first Super Admin if there isn't one (see DatabaseSeeder).
    /// Program.cs calls this only for "dotnet run --project src/Ghuri.Api -- seed".
    /// </summary>
    public static async Task SeedDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // A scope of its own, like one HTTP request gets - AppDbContext is
        // scoped and can't be resolved from the root provider.
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(cancellationToken);
    }

    /// <summary>
    /// Fills the catalogue with sample destinations and categories (see
    /// DemoDataSeeder). Program.cs allows it only in Development, for
    /// "dotnet run --project src/Ghuri.Api -- seed-demo".
    /// </summary>
    public static async Task SeedDemoDataAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(cancellationToken);
    }
}
