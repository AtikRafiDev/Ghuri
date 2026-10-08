using Ghuri.Domain.Entities.Catalog;
using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.Enums;
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

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Destinations.Exists(d => d.Id == id));

    public Task<bool> SlugExistsAsync(Slug slug, Guid? exceptId, CancellationToken cancellationToken) =>
        // Equals, not ==: Slug compares by value only through Equals (inside an
        // EF query == is translated to SQL, but in plain C# it compares references).
        Task.FromResult(Destinations.Exists(d => d.Slug.Equals(slug) && d.Id != exceptId));

    public Task<bool> CountryExistsAsync(short countryId, CancellationToken cancellationToken) =>
        Task.FromResult(CountryIds.Contains(countryId));

    public Task<bool> IsUsedByPackagesAsync(Guid destinationId, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task<IReadOnlyList<Destination>> GetFromSortOrderAsync(int sortOrder, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Destination>>(
            Destinations.Where(d => d.SortOrder >= sortOrder && d.Id != exceptId).OrderBy(d => d.SortOrder).ToList());

    public Task<int?> GetLastSortOrderAsync(Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Destinations.Where(d => d.Id != exceptId).Max(d => (int?)d.SortOrder));

    public void Add(Destination destination) => Destinations.Add(destination);
}

internal sealed class FakeCategoryRepository : ICategoryRepository
{
    /// <summary>The ids of categories that "exist".</summary>
    public HashSet<Guid> CategoryIds { get; } = [];

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Category?>(null);

    public Task<bool> NameExistsAsync(string name, Guid? exceptId, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<bool> SlugExistsAsync(Slug slug, Guid? exceptId, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<bool> IsUsedByPackagesAsync(Guid categoryId, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<bool> AllExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult(ids.All(CategoryIds.Contains));

    public void Add(Category category) => CategoryIds.Add(category.Id);
}

internal sealed class FakeTourPackageRepository : ITourPackageRepository
{
    private int _nextCode = 1001;

    public List<TourPackage> Packages { get; } = [];

    public Task<TourPackage?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Packages.Find(p => p.Id == id));

    public Task<TourPackage?> GetPublishedBySlugAsync(Slug slug, CancellationToken cancellationToken) =>
        Task.FromResult(Packages.Find(p => p.Slug.Equals(slug) && p.Status == PackageStatus.Published));

    public Task<bool> SlugExistsAsync(Slug slug, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Packages.Exists(p => p.Slug.Equals(slug) && p.Id != exceptId));

    /// <summary>Counts up like the real SQL SEQUENCE: PKG1001, PKG1002...</summary>
    public Task<string> NextPackageCodeAsync(CancellationToken cancellationToken) =>
        Task.FromResult($"PKG{_nextCode++}");

    public void Add(TourPackage package) => Packages.Add(package);
}

internal sealed class FakeDepartureRepository : IDepartureRepository
{
    public List<Departure> Departures { get; } = [];

    public Task<Departure?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Departures.Find(d => d.Id == id));

    public Task<IReadOnlyList<Departure>> ListForPackageAsync(Guid packageId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Departure>>(Departures.Where(d => d.PackageId == packageId).ToList());

    public Task<bool> ExistsOnDateAsync(Guid packageId, DateOnly startDate, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Departures.Exists(d => d.PackageId == packageId && d.StartDate == startDate && d.Id != exceptId));

    // Like the real one, it doesn't see unsaved changes: a departure added in
    // this handler call is skipped through exceptId, the same way.
    public Task<decimal?> LowestOpenAdultPriceAsync(Guid packageId, DateOnly today, Guid? exceptId, CancellationToken cancellationToken) =>
        Task.FromResult(Departures
            .Where(d => d.PackageId == packageId && d.Status == DepartureStatus.Open && d.StartDate >= today && d.Id != exceptId)
            .Select(d => (decimal?)d.AdultPrice)
            .Min());

    public Task<bool> HasOpenUpcomingAsync(Guid packageId, DateOnly today, CancellationToken cancellationToken) =>
        Task.FromResult(Departures.Exists(d => d.PackageId == packageId && d.Status == DepartureStatus.Open && d.StartDate >= today));

    // Seat counting is only meaningful against real SQL Server (row locks) -
    // see the integration test. No handler test should reach these.
    public Task<bool> TryReserveSeatsAsync(Guid departureId, short seats, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Tested against SQL Server in Ghuri.Api.IntegrationTests.");

    public Task<bool> ReleaseSeatsAsync(Guid departureId, short seats, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Tested against SQL Server in Ghuri.Api.IntegrationTests.");

    public void Add(Departure departure) => Departures.Add(departure);
}

internal sealed class FakeFileObjectRepository : IFileObjectRepository
{
    /// <summary>The ids of files that were "uploaded".</summary>
    public HashSet<Guid> FileIds { get; } = [];

    public Task<bool> AllExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult(ids.All(FileIds.Contains));

    public void Add(FileObject file) => FileIds.Add(file.Id);
}
