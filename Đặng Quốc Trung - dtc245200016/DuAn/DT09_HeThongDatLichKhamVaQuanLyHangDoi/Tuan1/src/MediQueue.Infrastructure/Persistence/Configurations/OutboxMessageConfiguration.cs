using MediQueue.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages", table =>
            table.HasCheckConstraint("CK_OutboxMessages_RetryCount", "[RetryCount] >= 0"));

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Type)
            .HasMaxLength(200)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(message => message.OccurredAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(message => message.PublishedAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(message => message.NextAttemptAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(message => message.LastError)
            .HasMaxLength(2000);

        builder.HasIndex(message => new
        {
            message.PublishedAtUtc,
            message.NextAttemptAtUtc,
            message.OccurredAtUtc
        })
            .HasFilter("[PublishedAtUtc] IS NULL")
            .HasDatabaseName("IX_OutboxMessages_Pending");
    }
}
