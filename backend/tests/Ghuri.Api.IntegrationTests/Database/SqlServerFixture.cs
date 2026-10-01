using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Ghuri.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// A real, empty SQL Server 2022 in Docker, with the REAL migrations
/// applied, and the real Api wired to it. Started once for a test class,
/// thrown away afterwards. Needs Docker running (Docker Desktop locally;
/// GitHub's ubuntu CI machines have it built in).
/// </summary>
/// <remarks>
/// Why not a fake: seat reservation depends on what SQL Server itself
/// does when 20 requests update one row at the same moment (row locks).
/// An in-memory list can't show that - only the real engine can.
/// </remarks>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private ContainerApiFactory? _factory;
    private Guid _destinationId;

    public IServiceProvider Services => _factory?.Services ?? throw new InvalidOperationException("Not started.");

    public async ValueTask InitializeAsync()
    {
        await _sql.StartAsync();
        _factory = new ContainerApiFactory(_sql.GetConnectionString());

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Safety net: these tests write data. If the Api were still pointed
        // at the development database, stop BEFORE touching it. Compares the
        // server address only - the password part may be rewritten.
        var expected = new SqlConnectionStringBuilder(_sql.GetConnectionString()).DataSource;
        var actual = new SqlConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
        if (actual != expected)
            throw new InvalidOperationException(
                $"The Api uses server '{actual}', not the test container '{expected}' - refusing to run.");

        await db.Database.MigrateAsync();

        // The minimum a departure needs above it: country → destination → package.
        var bangladesh = Country.Create("Bangladesh", "BD");
        db.Countries.Add(bangladesh);
        await db.SaveChangesAsync();

        var destination = Destination.Create(bangladesh.Id, "Cox's Bazar", Slug.Create("Cox's Bazar"));
        db.Destinations.Add(destination);
        await db.SaveChangesAsync();
        _destinationId = destination.Id;
    }

    /// <summary>A fresh fixed package with one departure of <paramref name="totalSeats"/> seats, saved. Returns the departure's id.</summary>
    public async Task<Guid> NewDepartureAsync(short totalSeats, bool closed = false)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var package = TourPackage.Create(
            $"T{unique}",
            new TourPackageDetails(_destinationId, $"Seat test {unique}", Slug.Create($"seat-test-{unique}"), "Summary", null,
                TourType.Group, [], [], null, null, false, null, null),
            PackagePricing.FixedDepartures(3, 2));
        var departure = Departure.Create(package.Id, new DateOnly(2030, 1, 1), 3, 10_000, 8_000, 0, null, totalSeats, 2);
        if (closed)
            departure.Close();

        db.TourPackages.Add(package);
        db.Departures.Add(departure);
        await db.SaveChangesAsync();
        return departure.Id;
    }

    /// <summary>ReservedSeats as stored in the database right now (a fresh, untracked read).</summary>
    public async Task<short> ReservedSeatsAsync(Guid departureId)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Departures.AsNoTracking().Where(d => d.Id == departureId).Select(d => d.ReservedSeats).SingleAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
        await _sql.DisposeAsync();
    }

    /// <summary>The usual test Api, with the connection string swapped for the container's.</summary>
    private sealed class ContainerApiFactory(string connectionString) : GhuriApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        }
    }
}
