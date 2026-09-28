using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Entities.Support;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Support;

internal sealed class CustomTourRequestConfiguration : IEntityTypeConfiguration<CustomTourRequest>
{
    public void Configure(EntityTypeBuilder<CustomTourRequest> builder)
    {
        builder.ToTable("CustomTourRequests", schema: "support");

        builder.HasKey(r => r.Id);
        builder.HasAuditColumns();

        builder.Property(r => r.RequestNo).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.HasIndex(r => r.RequestNo).IsUnique();

        builder.Property(r => r.UserId);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Name).HasMaxLength(150).IsRequired();

        builder.Property(r => r.Phone)
            .HasConversion(p => p.Value, v => PhoneNumber.Create(v))
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(r => r.Email).HasMaxLength(256);
        builder.Property(r => r.DestinationText).HasMaxLength(200).IsRequired();
        builder.Property(r => r.PreferredStartDate).HasColumnType("date");
        builder.Property(r => r.Days);
        builder.Property(r => r.Adults).IsRequired();
        builder.Property(r => r.Children).IsRequired();
        builder.Property(r => r.BudgetPerPerson).HasColumnType("decimal(18,2)");
        builder.Property(r => r.Message).HasMaxLength(2000);

        builder.Property(r => r.Status).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_CustomTourRequests_Status", "[Status] IN (1,2,3,4,5)"));
        builder.HasIndex(r => r.Status);

        builder.Property(r => r.AssignedTo);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.AssignedTo).OnDelete(DeleteBehavior.Restrict);
    }
}
