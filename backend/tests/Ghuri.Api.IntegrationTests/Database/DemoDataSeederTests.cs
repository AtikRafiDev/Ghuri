using Ghuri.Infrastructure.Persistence;
using Ghuri.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// Every demo destination from "seed-demo" gets a four-photo gallery: drawn
/// postcards while offline, swapped for real photos on a later run that can
/// download them - which is exactly what an existing development database
/// (seeded before real photos existed) goes through.
/// </summary>
public class DemoDataSeederTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    private async Task SeedAsync()
    {
        await using var scope = sql.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(TestContext.Current.CancellationToken);
    }

    private async Task<(List<string[]> Galleries, List<string> PackagePhotos, int FileRows)> ReadPhotosAsync()
    {
        await using var scope = sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ct = TestContext.Current.CancellationToken;

        var keys = await db.FileObjects.ToDictionaryAsync(f => f.Id, f => f.StorageKey, ct);
        var destinations = await db.Destinations.Include(d => d.Images).ToListAsync(ct);
        var packages = await db.TourPackages.Include(p => p.Images).ToListAsync(ct);

        return (
            destinations.Select(d => d.Images.Select(i => keys[i.FileId]).ToArray()).ToList(),
            packages.SelectMany(p => p.Images).Select(i => keys[i.FileId]).ToList(),
            keys.Count);
    }

    [Fact]
    public async Task EveryDestinationGetsFourPhotos_PostcardsOffline_RealPhotosOnceOnline()
    {
        // The fixture has only Bangladesh (international destinations are
        // skipped - their countries are missing), and "Cox's Bazar" with no photos.
        var source = sql.Services.GetRequiredService<FakeDemoPhotoSource>();

        // 1. Offline: drawn postcards, four per destination.
        source.Online = false;
        await SeedAsync();
        var (galleries, packagePhotos, fileRows) = await ReadPhotosAsync();

        Assert.Equal(17, galleries.Count); // the 17 in Bangladesh, "Cox's Bazar" filled in rather than duplicated
        Assert.All(galleries, g => Assert.Equal(4, g.Length));
        Assert.All(galleries.SelectMany(g => g), key => Assert.StartsWith(DemoPhotoWriter.PostcardFolder, key));
        Assert.Equal(4 * 3, packagePhotos.Count);
        Assert.Equal(17 * 4 + 4 * 3, fileRows);

        // Still offline: nothing changes, nothing is drawn twice.
        await SeedAsync();
        Assert.Equal(17 * 4 + 4 * 3, (await ReadPhotosAsync()).FileRows);

        // 2. Online: every postcard gallery - destinations and packages - becomes real photos.
        source.Online = true;
        await SeedAsync();
        (galleries, packagePhotos, fileRows) = await ReadPhotosAsync();

        Assert.All(galleries, g => Assert.Equal(4, g.Length));
        Assert.All(galleries.SelectMany(g => g).Concat(packagePhotos), key => Assert.StartsWith(DemoPhotoWriter.PhotoFolder, key));
        Assert.Equal(17 * 4 + 4 * 3, fileRows); // the postcards' rows are gone, not left behind

        // 3. Real photos are never replaced: another run downloads nothing.
        source.Requested.Clear();
        await SeedAsync();
        Assert.Empty(source.Requested);
    }
}
