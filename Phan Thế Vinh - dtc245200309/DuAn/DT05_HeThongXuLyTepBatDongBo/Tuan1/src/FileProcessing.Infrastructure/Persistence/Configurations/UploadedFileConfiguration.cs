using FileProcessing.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileProcessing.Infrastructure.Persistence.Configurations;

public class UploadedFileConfiguration : IEntityTypeConfiguration<UploadedFile>
{
    public void Configure(EntityTypeBuilder<UploadedFile> builder)
    {
        builder.ToTable("UploadedFiles");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(f => f.StoredFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(f => f.RelativePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(f => f.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(f => f.FileSize)
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .HasPrecision(3)
            .IsRequired();

        builder.HasMany(f => f.Jobs)
            .WithOne(j => j.File)
            .HasForeignKey(j => j.FileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

