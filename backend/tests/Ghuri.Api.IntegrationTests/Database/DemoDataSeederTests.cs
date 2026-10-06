using Ghuri.Infrastructure.Persistence;
using Ghuri.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// Every demo destination from "seed-demo" gets a photo gallery - including
/// one that was already in the database without photos (seeded by an older
/// version), which is what an existing development database looks like.
/// </summary>
public class DemoDataSeederTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    private async Task SeedAsync()
    {
        await using var scope = sql.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task EveryDestinationGetsFourPhotos_AndOldOnesWithoutPhotosAreFilledIn()
    {
        // The fixture has only Bangladesh, and "Cox's Bazar" with NO photos.
        // International destinations are skipped (their countries are missing).
        await SeedAsync();
        await SeedAsync(); // running it again adds nothing

        await using var scope = sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var destinations = await db.Destinations.Include(d => d.Images).ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(17, destinations.Count); // the 17 in Bangladesh
        Assert.All(destinations, d => Assert.Equal([0, 1, 2, 3], d.Images.Select(i => i.SortOrder)));
        Assert.Equal(4, destinations.Single(d => d.Name == "Cox's Bazar").Images.Count); // filled in, not duplicated

        // 4 per destination + 3 for each of the 4 demo packages, nothing twice.
        var photoCount = await db.FileObjects.CountAsync(f => f.StorageKey.StartsWith("images/demo/"), TestContext.Current.CancellationToken);
        Assert.Equal(17 * 4 + 4 * 3, photoCount);
    }
}
