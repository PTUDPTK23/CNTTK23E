using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Shared.Contracts;
using System.Text.Json;

namespace OrderWorker.Data;

public sealed class WorkerDbContext(DbContextOptions<WorkerDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<Order>();
        order.ToTable("orders");
        order.HasKey(x => x.Id);
        order.Property(x => x.Id).ValueGeneratedNever();
        order.Property(x => x.CustomerName).IsRequired();
        order.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var comparer = new ValueComparer<List<ProductItem>>(
            (a, b) => JsonSerializer.Serialize(a, jsonOptions) == JsonSerializer.Serialize(b, jsonOptions),
            a => JsonSerializer.Serialize(a, jsonOptions).GetHashCode(),
            a => JsonSerializer.Deserialize<List<ProductItem>>(JsonSerializer.Serialize(a, jsonOptions), jsonOptions)!);
        order.Property(x => x.Products)
            .HasColumnName("products")
            .HasColumnType("jsonb")
            .HasConversion(
                items => JsonSerializer.Serialize(items, jsonOptions),
                json => JsonSerializer.Deserialize<List<ProductItem>>(json, jsonOptions) ?? new(),
                comparer);
    }
}
