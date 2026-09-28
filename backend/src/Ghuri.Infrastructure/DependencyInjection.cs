using Ghuri.Application.Abstractions.Data;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ghuri.Infrastructure;

/// <summary>
/// Registers everything Infrastructure provides into the app's DI
/// container. Program.cs calls this ONE method instead of knowing about
/// AppDbContext, connection strings, or EF Core directly - that knowledge
/// stays inside Infrastructure, where it belongs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found in configuration.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Scoped, same as AppDbContext - the unit of work and the handler
        // must share the SAME DbContext instance within one request, or
        // SaveChanges would save a different context than the one the
        // handler changed.
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
