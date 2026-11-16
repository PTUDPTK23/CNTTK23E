using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Models;

namespace Shared.Contracts.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<Recipient> Recipients => Set<Recipient>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Campaigns table
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.ToTable("Campaigns");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(c => c.Title)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(c => c.Content)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(c => c.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(c => c.CreatedAt)
                .IsRequired();

            entity.HasIndex(c => c.CreatedAt);
            entity.HasIndex(c => c.Status);
        });

        // Recipients table
        modelBuilder.Entity<Recipient>(entity =>
        {
            entity.ToTable("Recipients");
            entity.HasKey(r => r.Id);

            entity.Property(r => r.FullName)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(r => r.Email)
                .HasMaxLength(255);

            entity.Property(r => r.PhoneNumber)
                .HasMaxLength(30);

            entity.Property(r => r.CreatedAt)
                .IsRequired();

            entity.HasIndex(r => r.Email);
            entity.HasIndex(r => r.PhoneNumber);
        });

        // NotificationDeliveries table
        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.ToTable("NotificationDeliveries");
            entity.HasKey(d => d.JobId);

            entity.Property(d => d.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(d => d.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(d => d.ErrorMessage)
                .HasColumnType("text");

            entity.Property(d => d.CreatedAt)
                .IsRequired();

            // Foreign Key: Campaign
            entity.HasOne(d => d.Campaign)
                .WithMany(c => c.Deliveries)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign Key: Recipient
            entity.HasOne(d => d.Recipient)
                .WithMany(r => r.Deliveries)
                .HasForeignKey(d => d.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for high-performance worker queue polling
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => new { d.Status, d.RetryCount });
            entity.HasIndex(d => d.CampaignId);
            entity.HasIndex(d => d.RecipientId);
        });
    }
}
