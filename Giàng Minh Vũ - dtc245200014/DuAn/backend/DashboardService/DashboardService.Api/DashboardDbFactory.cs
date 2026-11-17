using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public sealed class DashboardDbFactory : IDesignTimeDbContextFactory<DashboardDb>
{
    public DashboardDb CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Dashboard")
            ?? Environment.GetEnvironmentVariable("DASHBOARD_DB")
            ?? "Server=localhost,1433;Database=EventDashboard;Integrated Security=True;TrustServerCertificate=True;Encrypt=False";
        var options = new DbContextOptionsBuilder<DashboardDb>().UseSqlServer(connection).Options;
        return new DashboardDb(options);
    }
}
