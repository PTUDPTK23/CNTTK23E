using FileProcessing.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileProcessing.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.IdempotencyKey)
            .HasMaxLength(100);

        builder.Property(m => m.Payload)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(m => m.ScheduledAt)
            .HasPrecision(3);

        builder.Property(m => m.LockedUntil)
            .HasPrecision(3);

        builder.Property(m => m.RetryCount)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(m => m.ErrorMessage)
            .HasColumnType("nvarchar(max)");

        builder.Property(m => m.CreatedAt)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(m => m.ProcessedAt)
            .HasPrecision(3);

        // Filtered unique index on IdempotencyKey
        builder.HasIndex(m => m.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL")
            .HasDatabaseName("UQ_OutboxMessages_IdempotencyKey");

        // Index for Outbox Publisher polling and atomic batch claiming
        builder.HasIndex(m => new { m.Status, m.ScheduledAt, m.LockedUntil, m.CreatedAt })
            .HasDatabaseName("IX_OutboxMessages_Status_ScheduledAt_LockedUntil_CreatedAt");
    }
}

