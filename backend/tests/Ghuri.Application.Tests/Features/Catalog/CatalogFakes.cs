using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Tests.Features.Catalog;

// Hand-written stand-ins for the catalog repositories: plain lists instead of
// SQL Server, so a handler test runs in milliseconds.

internal sealed class FakeDestinationRepository : IDestinationRepository
{
    public List<Destination> Destinations { get; } = [];
    public HashSet<short> CountryIds { get; } = [];

    public Task<Destination?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Destinations.Find(d => d.Id == id));

    public Task<bool> SlugExistsAsync(Slug slug, Guid? exceptId, CancellationToken cancellationToken) =>
        // Equals, not ==: Slug compares by value only through Equals (inside an
        // EF query == is translated to SQL, but in plain C# it compares references).
        Task.FromResult(Destinations.Exists(d => d.Slug.Equals(slug) && d.Id != exceptId));

    public Task<bool> CountryExistsAsync(short countryId, CancellationToken cancellationToken) =>
        Task.FromResult(CountryIds.Contains(countryId));

    public Task<bool> IsUsedByPackagesAsync(Guid destinationId, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public void Add(Destination destination) => Destinations.Add(destination);
}

internal sealed class FakeFileObjectRepository : IFileObjectRepository
{
    /// <summary>The ids of files that were "uploaded".</summary>
    public HashSet<Guid> FileIds { get; } = [];

    public Task<bool> AllExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult(ids.All(FileIds.Contains));

    public void Add(FileObject file) => FileIds.Add(file.Id);
}
