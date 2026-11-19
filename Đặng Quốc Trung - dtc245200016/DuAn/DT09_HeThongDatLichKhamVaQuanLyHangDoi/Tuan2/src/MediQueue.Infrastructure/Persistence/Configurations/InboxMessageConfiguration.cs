using MediQueue.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(message => new { message.MessageId, message.ConsumerName });

        builder.Property(message => message.ConsumerName)
            .HasMaxLength(200)
            .IsUnicode(false);

        builder.Property(message => message.ProcessedAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(message => message.Result)
            .HasMaxLength(1000);

        builder.HasIndex(message => message.ProcessedAtUtc)
            .HasDatabaseName("IX_InboxMessages_ProcessedAtUtc");
    }
}
