using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Ghuri.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// A real, empty SQL Server database on LocalDB - the same SQL Server the
/// app uses on this PC - with the REAL migrations applied, and the real Api
/// wired to it. Created once for a test class, deleted afterwards. Needs
/// only SQL Server LocalDB (it comes with Visual Studio, and GitHub's
/// Windows CI machines have it too) - no Docker.
/// </summary>
/// <remarks>
/// Why not a fake: seat reservation depends on what SQL Server itself
/// does when 20 requests update one row at the same moment (row locks).
/// An in-memory list can't show that - only the real engine can.
/// Each fixture gets its OWN database (GhuriTest_&lt;random&gt;), so test
/// classes running in parallel never see each other's rows, and the
/// development database GhuriDb is never touched.
/// </remarks>
public sealed class SqlServerFixture : IAsyncLifetime
{
    /// <summary>Every test database starts with this, so a stray one is easy to spot (and the safety check below can rely on it).</summary>
    private const string TestDatabasePrefix = "GhuriTest_";

    private readonly string _connectionString = new SqlConnectionStringBuilder
    {
        DataSource = @"(localdb)\MSSQLLocalDB",
        InitialCatalog = TestDatabasePrefix + Guid.NewGuid().ToString("N"),
        IntegratedSecurity = true,
        TrustServerCertificate = true,
    }.ConnectionString;

    /// <summary>This fixture's own folder for saved files, deleted with the database.</summary>
    private readonly string _uploadsPath = Path.Combine(Path.GetTempPath(), "ghuri-test-uploads", Guid.NewGuid().ToString("N"));

    private TestDatabaseApiFactory? _factory;
    private Guid _destinationId;

    public IServiceProvider Services => _factory?.Services ?? throw new InvalidOperationException("Not started.");

    /// <summary>An HTTP client for the Api running on the test database.</summary>
    public HttpClient CreateClient() => _factory?.CreateClient() ?? throw new InvalidOperationException("Not started.");

    /// <summary>The same, but redirects are NOT followed - to check a 303 and its Location.</summary>
    public HttpClient CreateClientWithoutRedirects() =>
        _factory?.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })
        ?? throw new InvalidOperationException("Not started.");

    /// <summary>The stand-in for SSLCommerz the test Api uses (never the real gateway).</summary>
    public FakePaymentGateway PaymentGateway => Services.GetRequiredService<FakePaymentGateway>();

    public async ValueTask InitializeAsync()
    {
        _factory = new TestDatabaseApiFactory(_connectionString, _uploadsPath);

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Safety net: these tests write data, and DisposeAsync DELETES the
        // database. If the Api were still pointed at the development
        // database GhuriDb (same LocalDB server!), stop BEFORE touching it.
        var expected = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;
        var actual = new SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog;
        if (actual != expected || !actual.StartsWith(TestDatabasePrefix, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"The Api uses database '{actual}', not the test database '{expected}' - refusing to run.");

        // Creates the database, then applies every migration - exactly what
        // ".\ef.cmd database update" does to GhuriDb.
        await db.Database.MigrateAsync();

        // The minimum a departure needs above it: country → destination → package.
        var bangladesh = Country.Create("Bangladesh", "BD");
        db.Countries.Add(bangladesh);
        await db.SaveChangesAsync();
        CountryId = bangladesh.Id;

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

    /// <summary>Bangladesh, added at start-up - for tests that need their own destinations.</summary>
    public short CountryId { get; private set; }

    /// <summary>Adds these entities in one new scope and saves them - like one request would.</summary>
    public async Task SaveAsync(params object[] entities)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }

    /// <summary>Sends a query through the app's real pipeline (validation, logging) - no HTTP needed.</summary>
    public Task<Result<TResponse>> SendAsync<TResponse>(IQuery<TResponse> query, Guid? asUser = null) =>
        SendAsUserAsync(asUser, sender => sender.Send(query).AsTask());

    /// <summary>
    /// Sends a command through the real pipeline - validation, logging AND the
    /// transaction (commit on success, roll back on failure) - as if this user
    /// were logged in.
    /// </summary>
    public Task<Result<TResponse>> SendCommandAsync<TResponse>(ICommand<TResponse> command, Guid? asUser) =>
        SendAsUserAsync(asUser, sender => sender.Send(command).AsTask());

    /// <summary>The same, for a command that returns no value (e.g. a gateway callback - nobody is logged in for those).</summary>
    public async Task<Result> SendCommandAsync(ICommand command, Guid? asUser = null)
    {
        await using var scope = Services.CreateAsyncScope();
        var testUser = scope.ServiceProvider.GetRequiredService<TestCurrentUser>();
        testUser.Active = true;
        testUser.UserId = asUser;
        return await scope.ServiceProvider.GetRequiredService<Mediator.ISender>().Send(command);
    }

    private async Task<Result<TResponse>> SendAsUserAsync<TResponse>(Guid? userId, Func<Mediator.ISender, Task<Result<TResponse>>> send)
    {
        await using var scope = Services.CreateAsyncScope();
        var testUser = scope.ServiceProvider.GetRequiredService<TestCurrentUser>();
        testUser.Active = true;
        testUser.UserId = userId;
        return await send(scope.ServiceProvider.GetRequiredService<Mediator.ISender>());
    }

    /// <summary>A new customer account (no password). Returns its id.</summary>
    public async Task<Guid> NewCustomerAsync() => (await NewCustomerUserAsync()).Id;

    /// <summary>A new customer account - the whole User, e.g. to make a real access token for HTTP tests.</summary>
    public async Task<User> NewCustomerUserAsync()
    {
        // A random, valid, unique mobile number: 017 + 8 digits.
        var phone = PhoneNumber.Create("017" + Random.Shared.Next(0, 100_000_000).ToString("D8"));
        var customer = User.Create("Test Customer", phone, email: null, passwordHash: null);
        await SaveAsync(customer);
        return customer;
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
        if (_factory is null)
            return;

        // Delete the test database while the Api's services still exist,
        // so LocalDB doesn't fill up with old GhuriTest_ databases. EF
        // closes the open connections first. (A run killed half-way can
        // leave one behind - harmless; delete it in SSMS if you like.)
        await using (var scope = Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureDeletedAsync();
        }

        await _factory.DisposeAsync();

        if (Directory.Exists(_uploadsPath))
            Directory.Delete(_uploadsPath, recursive: true);
    }

    /// <summary>The usual test Api, with the connection string swapped for the test database's.</summary>
    private sealed class TestDatabaseApiFactory(string connectionString, string uploadsPath) : GhuriApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);

            // Files a test saves (e.g. the demo seeder's photos) go to a temp
            // folder - never into the developer's real uploads folder.
            builder.UseSetting("Storage:RootPath", uploadsPath);

            // "Who is logged in" without HTTP or tokens: SendAsync /
            // SendCommandAsync set it for their own scope. Every other scope -
            // a real HTTP request with a real token - keeps the Api's own
            // HttpCurrentUser (internal to the Api, so it's reached through
            // its registration's type rather than by name).
            builder.ConfigureTestServices(services =>
            {
                var httpCurrentUser = services.Last(d => d.ServiceType == typeof(ICurrentUser)).ImplementationType!;
                services.AddScoped(httpCurrentUser);
                services.AddScoped<TestCurrentUser>();
                services.AddScoped<ICurrentUser>(sp =>
                {
                    var testUser = sp.GetRequiredService<TestCurrentUser>();
                    return testUser.Active ? testUser : (ICurrentUser)sp.GetRequiredService(httpCurrentUser);
                });

                // Never call the real SSLCommerz from a test.
                services.AddSingleton<FakePaymentGateway>();
                services.AddSingleton<IPaymentGateway>(sp => sp.GetRequiredService<FakePaymentGateway>());
            });
        }
    }

    /// <summary>The logged-in user for one scope - set by SendAsUserAsync. UserId null = nobody (like an anonymous visitor).</summary>
    private sealed class TestCurrentUser : ICurrentUser
    {
        /// <summary>True only in scopes made by SendAsUserAsync; elsewhere the real HttpCurrentUser answers.</summary>
        public bool Active { get; set; }
        public Guid? UserId { get; set; }
        public IReadOnlyList<Ghuri.Domain.Enums.SystemRole> Roles => [];
        public bool IsInRole(Ghuri.Domain.Enums.SystemRole role) => false;
    }
}
