using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace ClinicQueue.Api.Data.Migrations;
[DbContext(typeof(ApiDbContext))]
public sealed class ApiDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.14");
        modelBuilder.HasDefaultSchema("api");
    }
}
