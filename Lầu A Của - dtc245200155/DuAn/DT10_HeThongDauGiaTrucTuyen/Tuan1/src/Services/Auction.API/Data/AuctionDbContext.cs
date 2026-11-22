using AuctionSystem.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Auction.API.Data;

public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options)
    {
    }

    public DbSet<AuctionSystem.Shared.Models.Auction> Auctions => Set<AuctionSystem.Shared.Models.Auction>();
    public DbSet<Bid> Bids => Set<Bid>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuctionSystem.Shared.Models.Auction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.StartingPrice).HasPrecision(18, 2);
            entity.Property(e => e.CurrentPrice).HasPrecision(18, 2);
            entity.Property(e => e.StepPrice).HasPrecision(18, 2);

            entity.HasMany(e => e.Bids)
                  .WithOne()
                  .HasForeignKey(b => b.AuctionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Bid>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Bidder).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasIndex(e => e.AuctionId);
            entity.HasIndex(e => e.BidTime);
        });
    }
}
