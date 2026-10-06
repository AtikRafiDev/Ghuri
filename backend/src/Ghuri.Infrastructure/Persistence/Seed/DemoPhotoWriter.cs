using System.Security.Cryptography;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Infrastructure.Persistence.Seed;

/// <summary>
/// DEVELOPMENT ONLY: draws demo postcards (DemoPostcard) and saves them like
/// uploaded photos - the file on disk + a FileObject row - so the demo
/// destinations and packages share one way of getting their photos.
/// </summary>
internal sealed class DemoPhotoWriter(AppDbContext db, IFileStorage storage)
{
    /// <summary>
    /// One postcard per caption (its large title and smaller subtitle), in
    /// order - the first is the cover. <paramref name="name"/> names the
    /// files, e.g. "cox-s-bazar-1.webp". Returns the new files' ids; the
    /// caller saves the changes.
    /// </summary>
    public async Task<List<Guid>> SaveAsync(
        string name, IReadOnlyList<(string Title, string Subtitle)> captions, string skyTop, string skyBottom,
        DateTime nowUtc, CancellationToken cancellationToken)
    {
        var fileIds = new List<Guid>();

        for (var variant = 0; variant < captions.Count; variant++)
        {
            var bytes = DemoPostcard.Draw(captions[variant].Title, captions[variant].Subtitle, skyTop, skyBottom, variant);
            var storageKey = $"images/demo/{Guid.CreateVersion7()}.webp";
            using (var content = new MemoryStream(bytes, writable: false))
                await storage.SaveAsync(storageKey, content, cancellationToken);

            var file = FileObject.Create(
                storageKey, $"{Slug.Create(name).Value}-{variant + 1}.webp", "image/webp", bytes.Length,
                Convert.ToHexStringLower(SHA256.HashData(bytes)), isPublic: true, nowUtc);
            db.FileObjects.Add(file);
            fileIds.Add(file.Id);
        }

        return fileIds;
    }
}
