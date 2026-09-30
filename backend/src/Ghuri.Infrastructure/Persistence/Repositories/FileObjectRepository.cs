using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.Repositories;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IFileObjectRepository.</summary>
internal sealed class FileObjectRepository(AppDbContext db) : IFileObjectRepository
{
    public void Add(FileObject file) => db.FileObjects.Add(file);
}
