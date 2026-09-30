using System.Text;
using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Repositories;
using Ghuri.Infrastructure.Images;
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
        services.AddFileStorage();

        return services;
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

        // The (serviceProvider, options) overload lets each DbContext pick
        // up the interceptor instance for ITS OWN request scope.
        services.AddDbContext<AppDbContext>((serviceProvider, options) => options
            .UseSqlServer(connectionString)
            .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>())
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

        // Only ever resolved by the seed methods below ("dotnet run -- seed" / "-- seed-demo").
        services.AddScoped<DatabaseSeeder>();
        services.AddScoped<DemoDataSeeder>();

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
            default:
                throw new InvalidOperationException(
                    $"Email:Sender '{sender}' is not supported. Use \"Log\" (development only - writes emails to the console).");
        }
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
