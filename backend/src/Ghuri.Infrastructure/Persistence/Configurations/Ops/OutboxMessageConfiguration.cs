using Ghuri.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ghuri.Infrastructure.Persistence.Configurations.Ops;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", schema: "ops");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasMaxLength(200).IsUnicode(false).IsRequired();
        builder.Property(m => m.PayloadJson).IsRequired(); // NVARCHAR(MAX)
        builder.Property(m => m.OccurredAtUtc).IsRequired();

        builder.Property(m => m.ProcessedAtUtc);
        // The dispatcher job's query is "give me everything still pending" -
        // a filtered index means that query only ever scans unprocessed
        // rows, not the whole (ever-growing) table.
        builder.HasIndex(m => m.ProcessedAtUtc).HasFilter("[ProcessedAtUtc] IS NULL");

        builder.Property(m => m.Attempts).IsRequired();
        builder.Property(m => m.Error).HasMaxLength(1000);
    }
}
