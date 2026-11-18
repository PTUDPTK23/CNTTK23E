using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationItem> Notifications => Set<NotificationItem>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Recipient).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RecipientRole).HasMaxLength(30);
            entity.Property(e => e.EventType).HasMaxLength(50);
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.HasIndex(e => e.Recipient);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventType).HasMaxLength(50);
        });
    }
}
