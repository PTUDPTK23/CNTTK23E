using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClinicQueue.Worker.Data;
public sealed class DatabaseHealthCheck(WorkerDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Database unavailable.");
}
