using FileProcessing.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileProcessing.Infrastructure.Persistence;

public class FileProcessingDbContext : DbContext
{
    public FileProcessingDbContext(DbContextOptions<FileProcessingDbContext> options)
        : base(options)
    {
    }

    public DbSet<UploadedFile> UploadedFiles => Set<UploadedFile>();
    public DbSet<ProcessingJob> ProcessingJobs => Set<ProcessingJob>();
    public DbSet<ProcessingResult> ProcessingResults => Set<ProcessingResult>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileProcessingDbContext).Assembly);
    }
}

