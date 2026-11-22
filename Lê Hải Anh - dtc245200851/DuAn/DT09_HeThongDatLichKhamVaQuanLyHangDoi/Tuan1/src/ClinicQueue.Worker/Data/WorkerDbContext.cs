using Microsoft.EntityFrameworkCore;

namespace ClinicQueue.Worker.Data;

// M0 skeleton: no business entities. This context owns only its service schema.
public sealed class WorkerDbContext(DbContextOptions<WorkerDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("worker");
    }
}
