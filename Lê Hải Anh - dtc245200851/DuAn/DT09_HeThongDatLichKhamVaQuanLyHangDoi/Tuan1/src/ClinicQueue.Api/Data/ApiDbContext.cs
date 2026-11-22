using Microsoft.EntityFrameworkCore;

namespace ClinicQueue.Api.Data;

// M0 skeleton: no business entities. This context owns only its service schema.
public sealed class ApiDbContext(DbContextOptions<ApiDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("api");
    }
}
