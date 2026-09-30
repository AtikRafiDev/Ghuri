using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ghuri.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IFileObjectRepository.</summary>
internal sealed class FileObjectRepository(AppDbContext db) : IFileObjectRepository
{
    public async Task<bool> AllExistAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return true;
        var distinct = ids.Distinct().ToList();
        // One query for the whole list: SELECT COUNT(*) ... WHERE Id IN (...)
        var found = await db.FileObjects.CountAsync(f => distinct.Contains(f.Id), cancellationToken);
        return found == distinct.Count;
    }

    public void Add(FileObject file) => db.FileObjects.Add(file);
}
