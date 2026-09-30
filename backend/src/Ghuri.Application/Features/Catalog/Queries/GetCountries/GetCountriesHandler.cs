using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Application.Features.Catalog.Queries.GetCountries;

internal sealed class GetCountriesHandler(IReadDbContext db) : IQueryHandler<GetCountriesQuery, IReadOnlyList<CountryDto>>
{
    public async ValueTask<Result<IReadOnlyList<CountryDto>>> Handle(GetCountriesQuery query, CancellationToken cancellationToken) =>
        await db.Countries
            .OrderBy(c => c.Name)
            .Select(c => new CountryDto(c.Id, c.Name, c.IsoCode))
            .ToListAsync(cancellationToken);
}
