using Ghuri.Application.Abstractions.Data;
using Ghuri.Infrastructure.Persistence;
using Ghuri.Infrastructure.Persistence.Interceptors;
using Ghuri.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found in configuration.");

        // The real clock. "Try" = only if nobody registered one already, so
        // a test can register a fixed fake clock first and keep it.
        services.TryAddSingleton(TimeProvider.System);

        // Scoped, not singleton: it depends on ICurrentUser, which changes
        // with every request.
        services.AddScoped<AuditableEntityInterceptor>();

        // The (serviceProvider, options) overload lets each DbContext pick
        // up the interceptor instance for ITS OWN request scope.
        services.AddDbContext<AppDbContext>((serviceProvider, options) => options
            .UseSqlServer(connectionString)
            .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>()));

        // Scoped, same as AppDbContext - the unit of work and the handler
        // must share the SAME DbContext instance within one request, or
        // SaveChanges would save a different context than the one the
        // handler changed.
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // The read side - a no-tracking view over the same AppDbContext.
        services.AddScoped<IReadDbContext, ReadDbContext>();

        // Only ever resolved by SeedDatabaseAsync below ("dotnet run -- seed").
        services.AddScoped<DatabaseSeeder>();

        // "Can we actually reach SQL Server?" - tagged "ready" so it only
        // runs on /health/ready, not on the lightweight /health/live
        // (see Program.cs for why the two are different).
        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(name: "database", tags: [HealthCheckTags.Ready]);

        return services;
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
}
