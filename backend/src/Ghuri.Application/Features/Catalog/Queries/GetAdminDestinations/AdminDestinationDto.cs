namespace Ghuri.Application.Features.Catalog.Queries.GetAdminDestinations;

/// <summary>Everything the edit dialog needs, so it can open without a second request.</summary>
/// <remarks>PackageCount above 0 = delete will be refused; the table can say why up front.</remarks>
public sealed record AdminDestinationDto(
    Guid Id,
    string Name,
    string Slug,
    string? Summary,
    short CountryId,
    string CountryName,
    string CountryIsoCode,
    bool IsInternational,
    Guid? ImageFileId,
    string? ImageUrl,
    bool IsFeatured,
    int SortOrder,
    string? SeoTitle,
    string? SeoDescription,
    int PackageCount);
