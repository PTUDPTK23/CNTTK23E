using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public sealed class AuditDbFactory : IDesignTimeDbContextFactory<AuditDb>
{
    public AuditDb CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Audit")
            ?? Environment.GetEnvironmentVariable("AUDIT_DB")
            ?? "Server=localhost,1433;Database=EventAudit;Integrated Security=True;TrustServerCertificate=True;Encrypt=False";
        var options = new DbContextOptionsBuilder<AuditDb>().UseSqlServer(connection).Options;
        return new AuditDb(options);
    }
}
