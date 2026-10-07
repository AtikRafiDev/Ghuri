using Ghuri.Domain.Common;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Cms;

/// <summary>A travel blog post / guide (blueprint: cms.BlogPosts [A][S]).</summary>
public sealed class BlogPost : AggregateRoot, IAuditable, ISoftDeletable
{
    public Slug Slug { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public string? Excerpt { get; private set; }
    public string Content { get; private set; } = string.Empty;

    /// <summary>FK to ops.FileObjects - not configured yet, see the note on User.AvatarFileId.</summary>
    public Guid? CoverFileId { get; private set; }

    public Guid AuthorId { get; private set; }

    /// <summary>Free-text, comma-separated for now - the blueprint doesn't normalize tags into their own table.</summary>
    public string? Tags { get; private set; }

    public bool IsPublished { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    private BlogPost()
    {
    }

    public static BlogPost Create(Slug slug, string title, string content, Guid authorId, string? excerpt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new BlogPost
        {
            Slug = slug,
            Title = title,
            Content = content,
            AuthorId = authorId,
            Excerpt = excerpt,
            IsPublished = false
        };
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = nowUtc;
    }
}
