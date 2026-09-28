namespace Ghuri.Domain.Enums;

/// <summary>Maps to catalog.TourPackages.Status (TINYINT).</summary>
public enum PackageStatus : byte
{
    Draft = 1,
    Published = 2,
    Archived = 3
}
