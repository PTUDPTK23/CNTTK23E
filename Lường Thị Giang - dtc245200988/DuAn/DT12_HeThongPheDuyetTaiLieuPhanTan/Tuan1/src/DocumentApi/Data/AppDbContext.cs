using DocumentApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<DocumentItem> Documents => Set<DocumentItem>();
    public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentItem>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Title).HasMaxLength(250).IsRequired();
            entity.Property(d => d.Category).HasMaxLength(100).IsRequired();
            entity.Property(d => d.Creator).HasMaxLength(120).IsRequired();
            entity.Property(d => d.Approver).HasMaxLength(120).IsRequired();
            entity.Property(d => d.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => d.Creator);
            entity.HasIndex(d => d.Approver);

            entity.HasMany(d => d.Histories)
                  .WithOne(h => h.Document)
                  .HasForeignKey(h => h.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApprovalHistory>(entity =>
        {
            entity.ToTable("approval_histories");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Action).HasMaxLength(50).IsRequired();
            entity.Property(h => h.FromStatus).HasMaxLength(30).IsRequired();
            entity.Property(h => h.ToStatus).HasMaxLength(30).IsRequired();
            entity.Property(h => h.PerformedBy).HasMaxLength(120).IsRequired();
            entity.Property(h => h.CorrelationId).HasMaxLength(64).IsRequired();
        });
    }
}
