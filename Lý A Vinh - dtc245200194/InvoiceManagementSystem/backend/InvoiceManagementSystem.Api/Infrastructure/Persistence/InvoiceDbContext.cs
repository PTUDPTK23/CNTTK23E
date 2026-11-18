using Microsoft.EntityFrameworkCore;

namespace InvoiceManagementSystem.Api.Infrastructure.Persistence;

/// <summary>
/// Central EF Core entry point. Entity sets and mappings will be added with the domain model.
/// </summary>
public sealed class InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<RoleDefinition> Roles => Set<RoleDefinition>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<UserAccount>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<RoleDefinition>().HasKey(x => x.Name);
        modelBuilder.Entity<Invoice>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<InventoryItem>().HasIndex(x => x.Sku).IsUnique();
        modelBuilder.Entity<Invoice>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<InventoryItem>().Property(x => x.UnitPrice).HasPrecision(18, 2);
    }
}
