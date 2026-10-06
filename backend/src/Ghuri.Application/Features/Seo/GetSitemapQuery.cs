using Ghuri.Application.Abstractions.Data;
using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Common;
using Ghuri.Application.Features.CustomTrips;
using Ghuri.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ghuri.Application.Features.Seo;

/// <summary>
/// The public pages search engines should know about (17-day plan, Day 16:
/// sitemap.xml) - the fixed pages plus every published package. Private
/// pages (account, admin, checkout) are never listed.
/// </summary>
public sealed record GetSitemapQuery : IQuery<SitemapDto>;

/// <summary>SiteUrl: the public address (Site:PublicUrl), for robots.txt's "Sitemap:" line. Urls are absolute.</summary>
public sealed record SitemapDto(string SiteUrl, IReadOnlyList<SitemapEntryDto> Entries);

/// <summary>LastModified: null for the fixed pages - "changes rarely, no date to give".</summary>
public sealed record SitemapEntryDto(string Url, DateOnly? LastModified);

internal sealed class GetSitemapHandler(IReadDbContext db, IOptions<SiteOptions> site) : IQueryHandler<GetSitemapQuery, SitemapDto>
{
    /// <summary>The public pages that exist whatever is in the database (the router's public routes).</summary>
    private static readonly string[] FixedPages = ["", "packages", "plan-trip", "about", "faq", "terms", "privacy", "refund-policy"];

    public async ValueTask<Result<SitemapDto>> Handle(GetSitemapQuery query, CancellationToken cancellationToken)
    {
        var packages = await db.TourPackages
            .Where(p => p.Status == PackageStatus.Published)
            .OrderBy(p => p.Slug)
            .Select(p => new
            {
                p.Slug,
                // UpdatedAtUtc is the audit interceptor's shadow column - null until the first edit after creation.
                Changed = EF.Property<DateTime?>(p, "UpdatedAtUtc") ?? EF.Property<DateTime>(p, "CreatedAtUtc")
            })
            .ToListAsync(cancellationToken);

        var entries = FixedPages.Select(path => new SitemapEntryDto(site.Value.Link(path), null))
            .Concat(packages.Select(p => new SitemapEntryDto(
                site.Value.Link($"packages/{Uri.EscapeDataString(p.Slug.Value)}"),
                DateOnly.FromDateTime(p.Changed))))
            .ToList();

        return new SitemapDto(site.Value.PublicUrl.TrimEnd('/'), entries);
    }
}
