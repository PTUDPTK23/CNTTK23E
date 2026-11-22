using DocumentApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentApi.Data;

public class DocumentDbContext : DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options) : base(options) { }

    public DbSet<DocumentItem> Documents => Set<DocumentItem>();
    public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Approver).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasMany(e => e.Histories)
                  .WithOne(h => h.Document)
                  .HasForeignKey(h => h.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApprovalHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Actor).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RoutingKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.HasIndex(e => e.ProcessedAt);
        });
    }
}
