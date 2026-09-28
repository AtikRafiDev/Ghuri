using Ghuri.Application.Abstractions.Data;
using Ghuri.Infrastructure.Persistence;
using Ghuri.Infrastructure.Persistence.Interceptors;
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

        return services;
    }
}
