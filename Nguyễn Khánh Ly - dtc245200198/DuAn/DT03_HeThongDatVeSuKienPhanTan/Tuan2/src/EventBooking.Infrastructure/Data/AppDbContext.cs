
using EventBooking.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventBooking.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<EventEntity> Events => Set<EventEntity>();

    public DbSet<UserEntity> Users => Set<UserEntity>();

    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Một người dùng có nhiều yêu cầu đặt vé
        modelBuilder.Entity<BookingEntity>()
            .HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId);

        // Một sự kiện có nhiều yêu cầu đặt vé
        modelBuilder.Entity<BookingEntity>()
            .HasOne(b => b.Event)
            .WithMany()
            .HasForeignKey(b => b.EventId);

        modelBuilder.Entity<EventEntity>()
            .Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<EventEntity>()
            .Property(e => e.Location)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<UserEntity>()
            .Property(u => u.FullName)
            .HasMaxLength(150)
            .IsRequired();

        modelBuilder.Entity<UserEntity>()
            .Property(u => u.Email)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<BookingEntity>()
            .Property(b => b.Status)
            .HasMaxLength(20)
            .IsRequired();
    }
}
