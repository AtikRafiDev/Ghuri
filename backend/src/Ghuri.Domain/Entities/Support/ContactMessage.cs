using Ghuri.Domain.Common;
using Ghuri.Domain.Enums;

namespace Ghuri.Domain.Entities.Support;

/// <summary>A contact form submission (blueprint: support.ContactMessages). Not marked [A] - CreatedAtUtc is a real property here, not a shadow column.</summary>
public sealed class ContactMessage : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Email { get; private set; }

    /// <summary>Optional and unvalidated (plain string, not PhoneNumber) - a visitor might not even be Bangladeshi.</summary>
    public string? Phone { get; private set; }

    public string Subject { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public ContactMessageStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private ContactMessage()
    {
    }

    public static ContactMessage Create(string name, string subject, string message, DateTime nowUtc, string? email = null, string? phone = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new ContactMessage
        {
            Name = name,
            Subject = subject,
            Message = message,
            Email = email,
            Phone = phone,
            Status = ContactMessageStatus.New,
            CreatedAtUtc = nowUtc
        };
    }
}
