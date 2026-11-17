using FileProcessing.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileProcessing.Infrastructure.Persistence.Configurations;

public class ProcessingResultConfiguration : IEntityTypeConfiguration<ProcessingResult>
{
    public void Configure(EntityTypeBuilder<ProcessingResult> builder)
    {
        builder.ToTable("ProcessingResults");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.RelativePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.FileSize)
            .IsRequired();

        builder.Property(r => r.Width)
            .IsRequired();

        builder.Property(r => r.Height)
            .IsRequired();

        builder.Property(r => r.DurationMs)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasPrecision(3)
            .IsRequired();

        // 1-to-1 relationship with ProcessingJob and Unique Constraint on JobId
        builder.HasOne(r => r.Job)
            .WithOne(j => j.Result)
            .HasForeignKey<ProcessingResult>(r => r.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.JobId)
            .IsUnique()
            .HasDatabaseName("UQ_ProcessingResults_JobId");
    }
}

