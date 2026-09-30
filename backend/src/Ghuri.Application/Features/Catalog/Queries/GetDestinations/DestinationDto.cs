namespace Ghuri.Application.Features.Catalog.Queries.GetDestinations;

public sealed record DestinationDto(
    Guid Id,
    string Name,
    string Slug,
    string? Summary,
    string CountryName,
    string CountryIsoCode,
    bool IsInternational,
    string? ImageUrl,
    bool IsFeatured);
