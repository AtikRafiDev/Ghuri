using Ghuri.Domain.Common;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Ghuri.ArchitectureTests;

/// <summary>
/// Rules about the EF Core model that are easy to break by accident and
/// only show up at runtime. Building the model needs no database.
/// </summary>
public class PersistenceModelTests
{
    private static IModel Model()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused") // never connects - only the model is built
            .Options;
        using var db = new AppDbContext(options);
        return db.Model;
    }

    [Fact]
    public void BaseEntityIds_AreNeverDatabaseGenerated()
    {
        // BaseEntity makes its Guid id in C#. If EF thinks ids are "generated
        // on add", a child added to an already-saved parent (a package photo,
        // a booking traveller) is UPDATEd instead of INSERTed and the save
        // fails with DbUpdateConcurrencyException. AppDbContext prevents that
        // for every BaseEntity - this test keeps it that way.
        var offenders = Model().GetEntityTypes()
            .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType))
            .Where(t => t.FindProperty(nameof(BaseEntity.Id))!.ValueGenerated != ValueGenerated.Never)
            .Select(t => t.ClrType.Name)
            .ToList();

        Assert.Empty(offenders);
    }
}
