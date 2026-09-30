using Ghuri.Application.Abstractions.Ports;
using Microsoft.Extensions.Options;

namespace Ghuri.Infrastructure.Storage;

/// <summary>
/// IFileStorage on a plain folder (blueprint: "local disk, served by Nginx").
/// Swapped for S3/Azure Blob later without touching any handler.
/// </summary>
internal sealed class LocalDiskFileStorage(IOptions<LocalDiskStorageOptions> options) : IFileStorage
{
    public async Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken)
    {
        var path = FullPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // CreateNew: if a file with this name somehow exists, fail loudly
        // instead of silently replacing someone else's image.
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
    }

    public string GetPublicUrl(string storageKey) => $"{options.Value.PublicBaseUrl}/{storageKey}";

    private string FullPath(string storageKey)
    {
        var root = options.Value.RootPath;
        var path = Path.GetFullPath(Path.Combine(root, storageKey));

        // Keys are built by our own code, never typed by a user - but even so,
        // a key containing "..\" must never write outside the uploads folder.
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Storage key '{storageKey}' points outside the storage folder.");

        return path;
    }
}
