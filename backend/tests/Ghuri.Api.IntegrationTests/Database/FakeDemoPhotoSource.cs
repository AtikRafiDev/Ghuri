using Ghuri.Infrastructure.Persistence.Seed;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// Stands in for Wikimedia in the demo seeders, so a test never downloads.
/// Offline by default (every download "fails", so postcards are drawn);
/// set Online to hand back a small generated picture as the "photo".
/// </summary>
internal sealed class FakeDemoPhotoSource : IDemoPhotoSource
{
    public bool Online { get; set; }

    public List<string> Requested { get; } = [];

    public Task<byte[]?> DownloadAsync(string commonsFile, CancellationToken cancellationToken)
    {
        lock (Requested)
            Requested.Add(commonsFile);

        // Any picture the image processor can read will do - a postcard is the quickest to make.
        return Task.FromResult(Online ? DemoPostcard.Draw(commonsFile, "test photo", "#0EA5E9", "#FDE68A", 0) : null);
    }
}
