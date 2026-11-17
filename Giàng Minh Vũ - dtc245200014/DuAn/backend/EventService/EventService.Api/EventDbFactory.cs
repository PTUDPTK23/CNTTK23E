using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public sealed class EventDbFactory : IDesignTimeDbContextFactory<EventDb>
{
    public EventDb CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Events")
            ?? Environment.GetEnvironmentVariable("EVENTS_DB")
            ?? "Server=localhost,1433;Database=EventManagement;Integrated Security=True;TrustServerCertificate=True;Encrypt=False";
        var options = new DbContextOptionsBuilder<EventDb>().UseSqlServer(connection).Options;
        return new EventDb(options);
    }
}
