using System;
using System.Threading.Tasks;
using EventFlow.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventFlow.WebClient.Services;

public class DashboardSignalRClient : IAsyncDisposable
{
    private readonly string _hubUrl;
    private readonly ILogger<DashboardSignalRClient> _logger;
    private HubConnection? _hubConnection;

    public event Action<DashboardStatsDto>? OnStatsReceived;
    public event Action<ActivityLogDto>? OnActivityReceived;
    public event Action<bool>? OnConnectionStatusChanged;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public DashboardSignalRClient(IConfiguration config, ILogger<DashboardSignalRClient> logger)
    {
        var dashboardUrl = config["Services:DashboardServiceUrl"] ?? "http://localhost:5001";
        _hubUrl = $"{dashboardUrl.TrimEnd('/')}/hubs/dashboard";
        _logger = logger;
    }

    public async Task StartAsync()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
        {
            return;
        }

        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                .Build();

            _hubConnection.On<DashboardStatsDto>("ReceiveDashboardStats", stats =>
            {
                _logger.LogInformation("SignalR: Received real-time Dashboard Stats");
                OnStatsReceived?.Invoke(stats);
            });

            _hubConnection.On<ActivityLogDto>("ReceiveEventActivity", activity =>
            {
                _logger.LogInformation("SignalR: Received real-time Activity ({Type})", activity.EventType);
                OnActivityReceived?.Invoke(activity);
            });

            _hubConnection.Reconnecting += error =>
            {
                _logger.LogWarning("SignalR Reconnecting: {Error}", error?.Message);
                OnConnectionStatusChanged?.Invoke(false);
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                _logger.LogInformation("SignalR Reconnected: {Id}", connectionId);
                OnConnectionStatusChanged?.Invoke(true);
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                _logger.LogWarning("SignalR Connection Closed: {Error}", error?.Message);
                OnConnectionStatusChanged?.Invoke(false);
                return Task.CompletedTask;
            };

            await _hubConnection.StartAsync();
            _logger.LogInformation("SignalR connected successfully to {HubUrl}", _hubUrl);
            OnConnectionStatusChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to connect to SignalR hub at {HubUrl}: {Message}", _hubUrl, ex.Message);
            OnConnectionStatusChanged?.Invoke(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
