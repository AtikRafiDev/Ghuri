using Ghuri.Domain.Entities.Ops;

namespace Ghuri.Domain.Repositories;

/// <summary>Uploaded files' metadata (ops.FileObjects). Only adding is needed so far.</summary>
public interface IFileObjectRepository
{
    void Add(FileObject file);
}
