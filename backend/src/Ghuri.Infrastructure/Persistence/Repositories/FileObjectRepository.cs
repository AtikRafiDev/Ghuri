using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IFileObjectRepository.</summary>
internal sealed class FileObjectRepository(AppDbContext db) : IFileObjectRepository
{
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.FileObjects.AnyAsync(f => f.Id == id, cancellationToken);

    public void Add(FileObject file) => db.FileObjects.Add(file);
}
