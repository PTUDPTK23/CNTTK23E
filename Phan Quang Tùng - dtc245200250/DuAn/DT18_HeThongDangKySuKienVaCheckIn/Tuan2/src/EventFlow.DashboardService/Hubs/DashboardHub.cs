using System;
using System.Threading.Tasks;
using EventFlow.DashboardService.Services;
using EventFlow.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EventFlow.DashboardService.Hubs;

public class DashboardHub : Hub
{
    private readonly ILogger<DashboardHub> _logger;
    private readonly IDashboardStateService _stateService;

    public DashboardHub(ILogger<DashboardHub> logger, IDashboardStateService stateService)
    {
        _logger = logger;
        _stateService = stateService;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR client connected: {ConnectionId}", Context.ConnectionId);
        // Gửi ngay số liệu thống kê hiện tại cho client mới kết nối
        var stats = _stateService.GetCurrentStats();
        await Clients.Caller.SendAsync("ReceiveDashboardStats", stats);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinEventGroup(string eventId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"event:{eventId}");
        _logger.LogInformation("Client {ConnectionId} joined group 'event:{EventId}'", Context.ConnectionId, eventId);
    }

    public async Task LeaveEventGroup(string eventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event:{eventId}");
        _logger.LogInformation("Client {ConnectionId} left group 'event:{EventId}'", Context.ConnectionId, eventId);
    }

    public async Task RequestLatestStats()
    {
        var stats = _stateService.GetCurrentStats();
        await Clients.Caller.SendAsync("ReceiveDashboardStats", stats);
    }
}
