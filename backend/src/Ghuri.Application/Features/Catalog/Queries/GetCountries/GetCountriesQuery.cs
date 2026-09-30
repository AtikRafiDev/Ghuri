using Ghuri.Application.Abstractions.Messaging;

namespace Ghuri.Application.Features.Catalog.Queries.GetCountries;

/// <summary>Every country, A-Z - for the destination form's country dropdown.</summary>
public sealed record GetCountriesQuery : IQuery<IReadOnlyList<CountryDto>>;
