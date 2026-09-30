using Ghuri.Domain.Entities.Ops;

namespace Ghuri.Domain.Repositories;

/// <summary>Uploaded files' metadata (ops.FileObjects).</summary>
public interface IFileObjectRepository
{
    /// <summary>Lets a handler reject an image id that was never uploaded, instead of a database FK error.</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    void Add(FileObject file);
}
