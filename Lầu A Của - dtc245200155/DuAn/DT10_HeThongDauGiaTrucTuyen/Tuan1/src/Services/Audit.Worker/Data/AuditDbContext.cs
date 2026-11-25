using AuctionSystem.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Audit.Worker.Data;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AuctionId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CorrelationId).HasMaxLength(100);
            entity.HasIndex(e => e.AuctionId);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
