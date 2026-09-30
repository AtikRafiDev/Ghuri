namespace Ghuri.Application.Abstractions.Ports;

/// <summary>
/// Where uploaded files' bytes live (blueprint 9.3). Local disk today; S3 or
/// Azure Blob later behind this same interface, with no handler changes.
/// </summary>
/// <remarks>
/// A storage key is a relative path like "images/2026/09/0199....webp" - it
/// is what ops.FileObjects.StorageKey stores, never a full URL, so moving to
/// a CDN later only changes GetPublicUrl, not the database.
/// </remarks>
public interface IFileStorage
{
    Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken);

    /// <summary>The address a browser loads the file from, e.g. "/files/images/2026/09/0199....webp".</summary>
    string GetPublicUrl(string storageKey);
}
