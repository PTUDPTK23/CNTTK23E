using MediQueue.Infrastructure.Identity;
using MediQueue.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MediQueue.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.Action)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(log => log.ResourceType)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(log => log.ResourceId)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(log => log.OccurredAtUtc)
            .HasColumnType("datetimeoffset(0)");

        builder.Property(log => log.Metadata)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(log => new { log.ResourceType, log.ResourceId, log.OccurredAtUtc })
            .HasDatabaseName("IX_AuditLogs_Resource_OccurredAt");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(log => log.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
