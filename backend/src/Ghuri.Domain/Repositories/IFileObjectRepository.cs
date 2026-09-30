using Ghuri.Domain.Entities.Ops;

namespace Ghuri.Domain.Repositories;

/// <summary>Uploaded files' metadata (ops.FileObjects).</summary>
public interface IFileObjectRepository
{
    /// <summary>Lets a handler reject image ids that were never uploaded, instead of a database FK error.</summary>
    Task<bool> AllExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    void Add(FileObject file);
}
