using FileProcessing.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileProcessing.Infrastructure.Persistence.Configurations;

public class ProcessingJobConfiguration : IEntityTypeConfiguration<ProcessingJob>
{
    public void Configure(EntityTypeBuilder<ProcessingJob> builder)
    {
        builder.ToTable("ProcessingJobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.JobType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(j => j.ParametersJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(j => j.RetryCount)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(j => j.MaxRetries)
            .HasDefaultValue(3)
            .IsRequired();

        builder.Property(j => j.NextAttemptAt)
            .HasPrecision(3);

        builder.Property(j => j.LeaseToken);

        builder.Property(j => j.WorkerId)
            .HasMaxLength(100);

        builder.Property(j => j.LockedUntil)
            .HasPrecision(3);

        builder.Property(j => j.ErrorMessage)
            .HasColumnType("nvarchar(max)");

        builder.Property(j => j.CreatedAt)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(j => j.StartedAt)
            .HasPrecision(3);

        builder.Property(j => j.CompletedAt)
            .HasPrecision(3);

        // Indexes
        builder.HasIndex(j => new { j.Status, j.NextAttemptAt, j.CreatedAt })
            .HasDatabaseName("IX_ProcessingJobs_Status_NextAttemptAt_CreatedAt");

        builder.HasIndex(j => j.FileId)
            .HasDatabaseName("IX_ProcessingJobs_FileId");
    }
}

