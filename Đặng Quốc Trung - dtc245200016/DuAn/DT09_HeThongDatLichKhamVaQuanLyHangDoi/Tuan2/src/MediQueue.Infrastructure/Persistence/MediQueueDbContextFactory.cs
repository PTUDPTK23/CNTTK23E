using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MediQueue.Infrastructure.Persistence;

public sealed class MediQueueDbContextFactory : IDesignTimeDbContextFactory<MediQueueDbContext>
{
    public MediQueueDbContext CreateDbContext(string[] args)
    {
        string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__MediQueue");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set the ConnectionStrings__MediQueue environment variable before using EF Core design-time tools.");
        }

        var options = new DbContextOptionsBuilder<MediQueueDbContext>()
            .UseSqlServer(connectionString, sql =>
                sql.MigrationsAssembly(typeof(MediQueueDbContext).Assembly.FullName))
            .Options;

        return new MediQueueDbContext(options);
    }
}
