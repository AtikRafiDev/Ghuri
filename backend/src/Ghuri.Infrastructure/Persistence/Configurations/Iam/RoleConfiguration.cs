using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
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

        // The fixed role list (blueprint section 2.1), shipped INSIDE a
        // migration: every database - yours, staging, production - gets
        // exactly these rows from ".\ef.cmd database update", with no
        // separate step to forget. Ids come from SystemRole so code and
        // data can never disagree about which number means which role.
        builder.HasData(
            Role.Create((byte)SystemRole.SuperAdmin, nameof(SystemRole.SuperAdmin), "Full control: settings, staff, reports"),
            Role.Create((byte)SystemRole.Manager, nameof(SystemRole.Manager), "Admin staff: packages, departures, bookings, content"),
            Role.Create((byte)SystemRole.Sales, nameof(SystemRole.Sales), "Admin staff: bookings and customers"),
            Role.Create((byte)SystemRole.Accounts, nameof(SystemRole.Accounts), "Admin staff: payments and refunds"),
            Role.Create((byte)SystemRole.Customer, nameof(SystemRole.Customer), "Searches, books and pays for tours"));
    }
}
