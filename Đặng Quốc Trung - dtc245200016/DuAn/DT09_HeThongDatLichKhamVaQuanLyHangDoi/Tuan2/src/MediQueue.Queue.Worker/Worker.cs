namespace MediQueue.Queue.Worker;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("MediQueue Queue Worker foundation started. Business consumers are not implemented yet.");

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
