using System.Net;
using Microsoft.Extensions.Logging;

namespace Ghuri.Infrastructure.Persistence.Seed;

/// <summary>DEVELOPMENT ONLY: where the demo seeders get real photos. Tests swap in one that is always offline.</summary>
internal interface IDemoPhotoSource
{
    /// <summary>The photo's bytes (a JPEG), or null when it can't be had - no internet, file gone, too many requests.</summary>
    Task<byte[]?> DownloadAsync(string commonsFile, CancellationToken cancellationToken);
}

/// <summary>
/// Downloads freely licensed photos from Wikimedia Commons (the photo library
/// behind Wikipedia) by their exact file name, at 1920 px wide.
/// </summary>
/// <remarks>
/// <para>
/// One request at a time with a short pause between them, and a longer wait
/// when Wikimedia answers 429 "too many requests" - they block clients that
/// hurry. The first network failure (no internet) stops all further tries,
/// so an offline seed falls back to postcards at once instead of waiting
/// for a timeout on every photo.
/// </para>
/// <para>
/// The photos are CC BY / CC BY-SA / public domain: fine for local demo
/// data. A public site would have to credit each photographer - upload
/// your own photos in the admin before going live.
/// </para>
/// </remarks>
internal sealed class WikimediaPhotoSource(ILogger<WikimediaPhotoSource> logger) : IDemoPhotoSource
{
    // One long-lived client for the whole run (Microsoft's guidance). Wikimedia
    // asks every client to say who it is in the User-Agent.
    private static readonly HttpClient Http = CreateClient();

    private static readonly TimeSpan Pause = TimeSpan.FromMilliseconds(500);
    private const int MaxAttempts = 4;

    private bool _offline;

    public async Task<byte[]?> DownloadAsync(string commonsFile, CancellationToken cancellationToken)
    {
        if (_offline)
            return null;

        // Special:FilePath redirects to the stored thumbnail; 1920 is one of
        // Wikimedia's standard sizes (and the upload pipeline's own limit).
        var url = $"https://commons.wikimedia.org/wiki/Special:FilePath/{Uri.EscapeDataString(commonsFile)}?width=1920";

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            await Task.Delay(Pause, cancellationToken);
            try
            {
                using var response = await Http.GetAsync(url, cancellationToken);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var wait = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(5 * attempt);
                    logger.LogInformation("Wikimedia asked us to slow down - waiting {Seconds}s.", (int)wait.TotalSeconds);
                    await Task.Delay(wait, cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Photo {File} not downloaded: HTTP {Status}.", commonsFile, (int)response.StatusCode);
                    return null;
                }

                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Can't reach Wikimedia ({Error}) - using drawn postcards for the rest of this run.", ex.Message);
                _offline = true;
                return null;
            }
        }

        logger.LogWarning("Photo {File} not downloaded: still rate-limited after {Attempts} tries.", commonsFile, MaxAttempts);
        return null;
    }

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("GhuriDemoSeeder/1.0 (local development seed data)");
        return http;
    }
}
