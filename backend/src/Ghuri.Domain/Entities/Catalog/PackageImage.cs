using Ghuri.Domain.Common;

namespace Ghuri.Domain.Entities.Catalog;

/// <summary>
/// One gallery image belonging to a package. "internal" on Create means
/// only code inside Ghuri.Domain can construct one directly - everyone
/// else (Application, Infrastructure) must go through
/// TourPackage.SetImages(...), which is what actually enforces "an image
/// can't exist without a package" as a real rule, not just a convention.
/// </summary>
public sealed class PackageImage : BaseEntity
{
    public Guid PackageId { get; private set; }

    /// <summary>FK to ops.FileObjects - the uploaded WebP.</summary>
    public Guid FileId { get; private set; }

    public string? Caption { get; private set; }

    /// <summary>0 = the cover (shown on cards and lists), then the gallery order - same as DestinationImage.</summary>
    public int SortOrder { get; private set; }

    private PackageImage()
    {
    }

    internal static PackageImage Create(Guid packageId, Guid fileId, int sortOrder) =>
        new() { PackageId = packageId, FileId = fileId, SortOrder = sortOrder };

    internal void MoveTo(int sortOrder) => SortOrder = sortOrder;
}
