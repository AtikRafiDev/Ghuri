using Ghuri.Domain.Common;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Entities.Cms;

/// <summary>A static page - About, FAQ, Terms, Privacy, Refund Policy (blueprint: cms.Pages [A][S]).</summary>
public sealed class Page : AggregateRoot, IAuditable, ISoftDeletable
{
    public Slug Slug { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;

    /// <summary>Sanitized HTML - the sanitizing itself is Infrastructure's IHtmlSanitizer, applied before this is ever set (Day 13 work).</summary>
    public string Content { get; private set; } = string.Empty;

    public bool IsPublished { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAtUtc { get; private set; }

    private Page()
    {
    }

    public static Page Create(Slug slug, string title, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new Page
        {
            Slug = slug,
            Title = title,
            Content = content,
            IsPublished = false
        };
    }

    public void MarkDeleted(DateTime nowUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = nowUtc;
    }
}
