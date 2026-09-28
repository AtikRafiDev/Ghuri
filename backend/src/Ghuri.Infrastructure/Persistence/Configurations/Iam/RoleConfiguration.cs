using Ghuri.Domain.Entities.Iam;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Iam;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", schema: "iam");

        builder.HasKey(r => r.Id);
        // No ValueGeneratedOnAdd() here, unlike Country - Role ids are
        // fixed, known constants (SystemRole.SuperAdmin = 1, etc.),
        // assigned by the seed script, never by SQL Server.
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();

        builder.Property(r => r.Description).HasMaxLength(200);
    }
}
