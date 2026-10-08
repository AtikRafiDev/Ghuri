using Ghuri.Application.Features.Catalog.Commands.CreateDestination;
using Ghuri.Application.Features.Catalog.Commands.UpdateDestination;
using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.ValueObjects;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// Unique sort order numbers (2026-10-08) against real SQL Server: the
/// destinations moved along must really be SAVED, in the same transaction
/// as the one being created or edited - which only a real database proves.
/// A class of its own, so its database holds no other test's destinations.
/// </summary>
public class DestinationSortOrderTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private async Task<Destination> SavedAsync(int sortOrder)
    {
        var unique = Unique();
        var destination = Destination.Create(sql.CountryId, $"Place {unique}", Slug.Create($"place-{unique}"), sortOrder: sortOrder);
        await sql.SaveAsync(destination);
        return destination;
    }

    /// <summary>The sort order as stored now, read in a fresh scope (nothing cached).</summary>
    private async Task<int> StoredSortOrderAsync(Guid id)
    {
        await using var scope = sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Destinations.AsNoTracking().Where(d => d.Id == id).Select(d => d.SortOrder).SingleAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Creating_OnATakenNumber_SavesTheOthersMovedAlong()
    {
        var (a, b, c) = (await SavedAsync(1_010), await SavedAsync(1_011), await SavedAsync(1_030));

        var created = await sql.SendCommandAsync(
            new CreateDestinationCommand(sql.CountryId, $"New {Unique()}", null, null, [], false, SortOrder: 1_010), asUser: null);

        Assert.True(created.IsSuccess);
        Assert.Equal(
            (1_010, 1_011, 1_012, 1_030),
            (await StoredSortOrderAsync(created.Value), await StoredSortOrderAsync(a.Id), await StoredSortOrderAsync(b.Id), await StoredSortOrderAsync(c.Id)));
    }

    [Fact]
    public async Task Editing_WithTheNumberCleared_MovesItAfterTheLast()
    {
        var destination = await SavedAsync(2_000);
        var last = await SavedAsync(90_000); // the highest number in this test database

        var result = await sql.SendCommandAsync(new UpdateDestinationCommand(
            destination.Id, sql.CountryId, destination.Name, null, null, [], false, SortOrder: null));

        Assert.True(result.IsSuccess);
        Assert.Equal(90_000 + Ghuri.Domain.Services.DisplayOrder.Step, await StoredSortOrderAsync(destination.Id));
        Assert.Equal(90_000, await StoredSortOrderAsync(last.Id));
    }
}
