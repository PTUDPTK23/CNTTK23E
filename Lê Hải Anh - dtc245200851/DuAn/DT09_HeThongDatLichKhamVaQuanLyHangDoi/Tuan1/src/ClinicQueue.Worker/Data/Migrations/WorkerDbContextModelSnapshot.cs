using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace ClinicQueue.Worker.Data.Migrations;
[DbContext(typeof(WorkerDbContext))]
public sealed class WorkerDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.14");
        modelBuilder.HasDefaultSchema("worker");
    }
}
