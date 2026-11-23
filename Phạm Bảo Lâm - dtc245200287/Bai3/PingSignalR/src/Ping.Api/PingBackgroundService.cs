using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class PingBackgroundService : BackgroundService
{
    private readonly IHubContext<PingHub> _hubContext;
    private readonly ILogger<PingBackgroundService> _logger;

    public PingBackgroundService(IHubContext<PingHub> hubContext, ILogger<PingBackgroundService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PingBackgroundService đang chạy...");

        while (!stoppingToken.IsCancellationRequested)
        {
            string timeMessage = $"[Server Tick] Thoi gian he thong: {DateTime.Now:HH:mm:ss}";
            await _hubContext.Clients.All.SendAsync("ReceiveTick", timeMessage, stoppingToken);

            await Task.Delay(5000, stoppingToken);
        }
    }
}