using System.Security.Cryptography;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence.Seed;

/// <summary>
/// DEVELOPMENT ONLY: gets the demo destinations' and packages' photos and
/// saves them like uploaded ones - the file on disk + a FileObject row.
/// </summary>
/// <remarks>
/// Each photo is a real one from Wikimedia Commons (IDemoPhotoSource), put
/// through the same resize-to-WebP step as an admin upload. When it can't be
/// downloaded - offline, or in the tests - a drawn postcard (DemoPostcard)
/// stands in. The two are kept in different folders, so a later run can
/// tell a gallery of stand-ins from real photos and upgrade it.
/// </remarks>
internal sealed class DemoPhotoWriter(AppDbContext db, IFileStorage storage, IImageProcessor images, IDemoPhotoSource source)
{
    /// <summary>One photo to get: its Wikimedia Commons file, and the caption written on its postcard stand-in.</summary>
    public sealed record Shot(string CommonsFile, string Title, string Subtitle);

    /// <summary>Where drawn postcards are stored (also every demo photo made before real ones existed).</summary>
    public const string PostcardFolder = "images/demo/";

    /// <summary>Where downloaded real photos are stored.</summary>
    public const string PhotoFolder = "images/sample/";

    /// <summary>The same limit as an admin upload (UploadFileHandler).</summary>
    private const int MaxLongEdgePixels = 1920;

    /// <summary>
    /// The photos in order (the first is the cover). Returns the new files'
    /// ids; the caller saves the changes. <paramref name="allowPostcards"/>
    /// false = a photo that can't be downloaded is left out rather than drawn
    /// (used when replacing postcards - another set of them would not help).
    /// </summary>
    public async Task<List<Guid>> SaveAsync(
        string name, IReadOnlyList<Shot> shots, string skyTop, string skyBottom, bool allowPostcards,
        DateTime nowUtc, CancellationToken cancellationToken)
    {
        var fileIds = new List<Guid>();

        for (var i = 0; i < shots.Count; i++)
        {
            var shot = shots[i];
            var photo = await DownloadAsWebPAsync(shot.CommonsFile, cancellationToken);
            if (photo is not null)
            {
                // OriginalName = the Commons file, so where each photo came from (and whom to credit) stays known.
                fileIds.Add(await SaveFileAsync(PhotoFolder, shot.CommonsFile, photo, nowUtc, cancellationToken));
            }
            else if (allowPostcards)
            {
                var postcard = DemoPostcard.Draw(shot.Title, shot.Subtitle, skyTop, skyBottom, i);
                fileIds.Add(await SaveFileAsync(PostcardFolder, $"{Slug.Create(name).Value}-{i + 1}.webp", postcard, nowUtc, cancellationToken));
            }
        }

        return fileIds;
    }

    /// <summary>True when every one of these files is a drawn postcard - a gallery worth replacing with real photos.</summary>
    public async Task<bool> AllPostcardsAsync(IReadOnlyCollection<Guid> fileIds, CancellationToken cancellationToken) =>
        fileIds.Count > 0
        && await db.FileObjects.CountAsync(f => fileIds.Contains(f.Id) && f.StorageKey.StartsWith(PostcardFolder), cancellationToken) == fileIds.Count;

    /// <summary>
    /// Deletes the FileObject rows of postcards no gallery uses any more (in
    /// the same save that takes them out of the gallery). Their files stay
    /// on disk - IFileStorage has no delete - harmless, a few KB each.
    /// </summary>
    public async Task ForgetAsync(IReadOnlyCollection<Guid> fileIds, CancellationToken cancellationToken) =>
        db.FileObjects.RemoveRange(await db.FileObjects.Where(f => fileIds.Contains(f.Id)).ToListAsync(cancellationToken));

    private async Task<byte[]?> DownloadAsWebPAsync(string commonsFile, CancellationToken cancellationToken)
    {
        var bytes = await source.DownloadAsync(commonsFile, cancellationToken);
        if (bytes is null)
            return null;

        using var content = new MemoryStream(bytes, writable: false);
        var image = await images.ResizeToWebPAsync(content, MaxLongEdgePixels, cancellationToken);
        return image?.Bytes;
    }

    private async Task<Guid> SaveFileAsync(string folder, string originalName, byte[] bytes, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var storageKey = $"{folder}{Guid.CreateVersion7()}.webp";
        using (var content = new MemoryStream(bytes, writable: false))
            await storage.SaveAsync(storageKey, content, cancellationToken);

        var file = FileObject.Create(
            storageKey, originalName, "image/webp", bytes.Length,
            Convert.ToHexStringLower(SHA256.HashData(bytes)), isPublic: true, nowUtc);
        db.FileObjects.Add(file);
        return file.Id;
    }
}
