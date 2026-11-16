using EventFlow.AuditWorker.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.AuditWorker.Data;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.EventId);
            entity.HasIndex(a => a.EventType);
            entity.HasIndex(a => a.Timestamp);
            entity.HasIndex(a => a.CorrelationId);
            if (!string.IsNullOrEmpty(entity.Metadata.FindProperty("RegistrationCode")?.Name))
            {
                entity.HasIndex(a => a.RegistrationCode);
            }
        });
    }
}
