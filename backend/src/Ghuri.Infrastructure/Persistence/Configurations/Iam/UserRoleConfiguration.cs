using Ghuri.Domain.Entities.Iam;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Iam;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", schema: "iam");

        // Composite primary key - a user can't be assigned the same role
        // twice, and there's no reason for a surrogate id here.
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        // WithMany(u => u.Roles): the same foreign key as before, now also
        // reachable as user.Roles - EF fills the private _roles list (found
        // by naming convention) when a user is loaded with its roles.
        builder.HasOne<User>()
            .WithMany(u => u.Roles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade); // deleting a user removes their role assignments

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict); // never delete a Role while someone still has it

        builder.Property(ur => ur.AssignedAtUtc).IsRequired();
    }
}
