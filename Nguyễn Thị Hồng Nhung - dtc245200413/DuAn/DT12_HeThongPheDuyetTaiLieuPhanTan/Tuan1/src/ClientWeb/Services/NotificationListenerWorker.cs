using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace ClientWeb.Services;

public class NotificationListenerWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationListenerWorker> _logger;
    private readonly ConcurrentQueue<JsonElement> _liveEvents = new();

    public NotificationListenerWorker(IConfiguration configuration, ILogger<NotificationListenerWorker> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public IReadOnlyCollection<JsonElement> GetRecentLiveEvents() => _liveEvents.ToArray();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var notifUrl = (_configuration["Services:NotificationServiceUrl"] ?? "http://localhost:5002").TrimEnd('/');
        var hubUrl = $"{notifUrl}/hubs/notifications";

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        connection.On<JsonElement>("ReceiveNotification", payload =>
        {
            _liveEvents.Enqueue(payload.Clone());
            while (_liveEvents.Count > 50)
            {
                _liveEvents.TryDequeue(out _);
            }

            _logger.LogInformation("[SignalR ClientWeb] Received real-time notification from NotificationService: {Payload}",
                payload.GetRawText());
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await connection.StartAsync(stoppingToken);
                _logger.LogInformation("[SignalR ClientWeb] Connected to NotificationHub at {HubUrl}", hubUrl);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Cannot connect to NotificationHub at {HubUrl} ({Error}). Retrying in 3s...", hubUrl, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        await connection.DisposeAsync();
    }
}
