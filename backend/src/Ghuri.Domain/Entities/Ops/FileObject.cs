using Ghuri.Domain.Common;

namespace Ghuri.Domain.Entities.Ops;

/// <summary>Metadata for one uploaded file (blueprint: ops.FileObjects). The actual bytes live in blob/disk storage (IFileStorage, an Infrastructure port) - this row just tracks what and where.</summary>
public sealed class FileObject : BaseEntity
{
    /// <summary>The path/key inside blob storage - not the file's public URL.</summary>
    public string StorageKey { get; private set; } = string.Empty;

    public string OriginalName { get; private set; } = string.Empty;

    /// <summary>Checked by magic bytes on upload (Infrastructure), not trusted from the client-supplied filename.</summary>
    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;

    /// <summary>Public images are served via CDN; private documents need a signed URL to view.</summary>
    public bool IsPublic { get; private set; }

    public Guid? UploadedBy { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private FileObject()
    {
    }

    public static FileObject Create(
        string storageKey, string originalName, string contentType, long sizeBytes, string sha256, bool isPublic,
        DateTime nowUtc, Guid? uploadedBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);
        if (sizeBytes > 10 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Files larger than 10 MB are not allowed.");

        return new FileObject
        {
            StorageKey = storageKey,
            OriginalName = originalName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            Sha256 = sha256,
            IsPublic = isPublic,
            UploadedBy = uploadedBy,
            CreatedAtUtc = nowUtc
        };
    }
}
