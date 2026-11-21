namespace Notification.Worker;

public class WorkerSettings
{
    public int BatchSize { get; set; } = 10;
    public int PollIntervalSeconds { get; set; } = 3;
    public int MaxRetries { get; set; } = 3;
    public int SimulationDelayMs { get; set; } = 300;
    public int SimulateFailureRatePercent { get; set; } = 15;
}
