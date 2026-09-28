using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Entities.Ops;
using Ghuri.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Iam;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", schema: "iam");

        builder.HasKey(u => u.Id);
        builder.HasAuditColumns();

        builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();

        builder.Property(u => u.Email).HasMaxLength(256);
        // Filtered unique index: two rows CAN both have a NULL email
        // (most customers won't set one), but if two rows both have a
        // real email, it must be different for each.
        builder.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL");

        builder.Property(u => u.NormalizedEmail).HasMaxLength(256);
        builder.HasIndex(u => u.NormalizedEmail);

        // PhoneNumber is a value object (Ghuri.Domain.ValueObjects), not a
        // primitive - HasConversion tells EF Core how to turn it into a
        // plain string column and back, so the rest of the app can keep
        // using the richer PhoneNumber type instead of a raw string.
        builder.Property(u => u.PhoneNumber)
            .HasConversion(p => p.Value, v => PhoneNumber.Create(v))
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(u => u.PhoneNumber).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(512);

        builder.Property(u => u.SecurityStamp).HasMaxLength(64).IsUnicode(false).IsRequired();

        builder.Property(u => u.EmailConfirmed).IsRequired();
        builder.Property(u => u.PhoneConfirmed).IsRequired();

        builder.Property(u => u.GoogleSubject).HasMaxLength(100).IsUnicode(false);
        builder.HasIndex(u => u.GoogleSubject).IsUnique().HasFilter("[GoogleSubject] IS NOT NULL");

        // Was a plain column with no FK while ops hadn't been built yet -
        // now that FileObject exists, the real constraint goes here.
        builder.Property(u => u.AvatarFileId);
        builder.HasOne<FileObject>().WithMany().HasForeignKey(u => u.AvatarFileId).OnDelete(DeleteBehavior.SetNull);

        // Enum -> TINYINT, with an explicit CHECK so a bad value can never
        // land in the column even from outside EF Core.
        builder.Property(u => u.Status).HasConversion<byte>().IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("CK_Users_Status", "[Status] IN (1,2,3)"));

        builder.Property(u => u.AccessFailedCount).IsRequired();
        builder.Property(u => u.LockoutEndUtc);
        builder.Property(u => u.LastLoginUtc);
    }
}
