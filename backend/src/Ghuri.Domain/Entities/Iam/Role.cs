namespace Ghuri.Domain.Entities.Iam;

/// <summary>
/// A fixed, seeded role (SuperAdmin, Manager, Sales, Accounts, Customer).
/// Not a BaseEntity - like Country, its id is a small database-assigned
/// number (here, a fixed constant from SystemRole), not an app-generated
/// Guid, since the row list never grows through the admin panel.
/// </summary>
public sealed class Role
{
    public byte Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Role()
    {
    }

    public static Role Create(byte id, string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Role { Id = id, Name = name, Description = description };
    }
}
