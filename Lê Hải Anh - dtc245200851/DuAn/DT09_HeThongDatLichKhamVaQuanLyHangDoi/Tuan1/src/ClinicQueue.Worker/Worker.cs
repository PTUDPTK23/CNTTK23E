using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace ClinicQueue.Worker;

// Health heartbeat only; no RabbitMQ consumer or queue writes in M0.
public sealed class Worker(HealthCheckService health, IConfiguration config, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var path = config["Health:HeartbeatPath"] ?? Path.Combine(Path.GetTempPath(), "clinicqueue-worker.healthy");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        try
        {
            do
            {
                var report = await health.CheckHealthAsync(stoppingToken);
                if (report.Status == HealthStatus.Healthy)
                    await File.WriteAllTextAsync(path, DateTimeOffset.UtcNow.ToString("O"), stoppingToken);
                else
                {
                    File.Delete(path);
                    logger.LogWarning("Worker readiness: {Status}", report.Status);
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        finally { File.Delete(path); }
    }
}
