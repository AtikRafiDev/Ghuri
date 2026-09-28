using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Cms;

/// <summary>A home page slider or offer banner (blueprint: cms.Banners [A][S]).</summary>
public sealed class Banner : AggregateRoot, IAuditable, ISoftDeletable
{
    public string Title { get; private set; } = string.Empty;
    public string? Subtitle { get; private set; }

    /// <summary>FK to ops.FileObjects - not configured yet, see the note on User.AvatarFileId.</summary>
    public Guid ImageFileId { get; private set; }

    public string? LinkUrl { get; private set; }
    public BannerPlacement Placement { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime? StartsAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    private Banner()
    {
    }

    public static Banner Create(string title, Guid imageFileId, BannerPlacement placement, string? subtitle = null, string? linkUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new Banner
        {
            Title = title,
            ImageFileId = imageFileId,
            Placement = placement,
            Subtitle = subtitle,
            LinkUrl = linkUrl,
            SortOrder = 0,
            IsActive = true
        };
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = nowUtc;
    }
}
