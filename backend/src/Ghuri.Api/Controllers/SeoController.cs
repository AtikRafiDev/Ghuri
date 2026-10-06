using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Ghuri.Application.Features.Seo;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ghuri.Api.Controllers;

/// <summary>
/// /sitemap.xml and /robots.txt for search engines (17-day plan, Day 16).
/// Served by the API - not files in the website - because the sitemap lists
/// every published package, which only the database knows. Locally Vite
/// forwards both paths here; in production Nginx must too.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class SeoController(ISender sender) : ControllerBase
{
    private static readonly XNamespace Ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

    /// <summary>The sitemap protocol (sitemaps.org): one &lt;url&gt; per public page, with &lt;lastmod&gt; when known.</summary>
    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600)] // search engines needn't re-fetch more than hourly
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var sitemap = (await sender.Send(new GetSitemapQuery(), cancellationToken)).Value;

        var xml = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(Ns + "urlset",
                sitemap.Entries.Select(e => new XElement(Ns + "url",
                    new XElement(Ns + "loc", e.Url),
                    e.LastModified is { } date ? new XElement(Ns + "lastmod", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) : null))));

        return Content(xml.Declaration + Environment.NewLine + xml.Root, "application/xml", Encoding.UTF8);
    }

    /// <summary>
    /// Crawl the public site, skip the private parts (they need a login and
    /// would only show "please log in"), and here is the sitemap.
    /// </summary>
    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Robots(CancellationToken cancellationToken)
    {
        var sitemap = (await sender.Send(new GetSitemapQuery(), cancellationToken)).Value;

        var robots = $"""
            User-agent: *
            Disallow: /account
            Disallow: /admin
            Disallow: /checkout
            Disallow: /payment
            Disallow: /api/

            Sitemap: {sitemap.SiteUrl}/sitemap.xml
            """;
        return Content(robots, "text/plain", Encoding.UTF8);
    }
}
