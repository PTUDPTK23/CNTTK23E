using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.DashboardService.Hubs;
using EventFlow.Shared.Messaging;
using EventFlow.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EventFlow.DashboardService.Services;

public class DashboardStateService : IDashboardStateService
{
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DashboardStateService> _logger;
    private readonly object _lock = new();

    private int _totalEvents = 5;
    private int _totalRegistrations = 0;
    private int _totalCheckedIn = 0;
    private int _totalRemainingSpots = 1010;
    private readonly List<ActivityLogDto> _recentActivities = new();

    public DashboardStateService(
        IHubContext<DashboardHub> hubContext,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DashboardStateService> logger)
    {
        _hubContext = hubContext;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public DashboardStatsDto GetCurrentStats()
    {
        lock (_lock)
        {
            return new DashboardStatsDto
            {
                TotalEvents = _totalEvents,
                TotalRegistrations = _totalRegistrations,
                TotalCheckedIn = _totalCheckedIn,
                TotalRemainingSpots = _totalRemainingSpots,
                LastUpdated = DateTime.UtcNow,
                RecentActivities = _recentActivities.Take(15).ToList()
            };
        }
    }

    public async Task InitializeFromEventApiAsync()
    {
        try
        {
            var eventApiUrl = _configuration["Services:EventApiUrl"] ?? "http://localhost:5000";
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(eventApiUrl);
            client.Timeout = TimeSpan.FromSeconds(5);

            var stats = await client.GetFromJsonAsync<DashboardStatsDto>("/api/dashboard/stats");
            if (stats != null)
            {
                lock (_lock)
                {
                    _totalEvents = stats.TotalEvents;
                    _totalRegistrations = stats.TotalRegistrations;
                    _totalCheckedIn = stats.TotalCheckedIn;
                    _totalRemainingSpots = stats.TotalRemainingSpots;
                }
                _logger.LogInformation("Dashboard state initialized from EventApi: Events={E}, Regs={R}, CheckedIn={C}", 
                    stats.TotalEvents, stats.TotalRegistrations, stats.TotalCheckedIn);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not fetch baseline stats from EventApi: {Msg}. Using default/in-memory state.", ex.Message);
        }
    }

    public async Task HandleParticipantRegisteredAsync(ParticipantRegisteredMessage message)
    {
        DashboardStatsDto stats;
        ActivityLogDto activity;

        lock (_lock)
        {
            _totalRegistrations++;
            if (_totalRemainingSpots > 0) _totalRemainingSpots--;

            activity = new ActivityLogDto
            {
                EventType = "ParticipantRegistered",
                Description = $"Người tham dự '{message.ParticipantName}' đã đăng ký thành công (Mã: {message.RegistrationCode})",
                EventName = message.EventName,
                ParticipantName = message.ParticipantName,
                Timestamp = message.Timestamp,
                CorrelationId = message.CorrelationId
            };

            _recentActivities.Insert(0, activity);
            if (_recentActivities.Count > 50) _recentActivities.RemoveAt(_recentActivities.Count - 1);

            stats = new DashboardStatsDto
            {
                TotalEvents = _totalEvents,
                TotalRegistrations = _totalRegistrations,
                TotalCheckedIn = _totalCheckedIn,
                TotalRemainingSpots = _totalRemainingSpots,
                LastUpdated = DateTime.UtcNow,
                RecentActivities = _recentActivities.Take(15).ToList()
            };
        }

        // Broadcast to all connected SignalR clients
        await _hubContext.Clients.All.SendAsync("ReceiveDashboardStats", stats);
        await _hubContext.Clients.All.SendAsync("ReceiveEventActivity", activity);

        _logger.LogInformation("Broadcasted real-time Registration event: {Code} for {Event}", message.RegistrationCode, message.EventName);
    }

    public async Task HandleParticipantCancelledAsync(ParticipantCancelledMessage message)
    {
        DashboardStatsDto stats;
        ActivityLogDto activity;

        lock (_lock)
        {
            if (_totalRegistrations > 0) _totalRegistrations--;
            _totalRemainingSpots++;

            activity = new ActivityLogDto
            {
                EventType = "ParticipantCancelled",
                Description = $"Người tham dự '{message.ParticipantName}' đã hủy vé (Mã: {message.RegistrationCode})",
                EventName = message.EventName,
                ParticipantName = message.ParticipantName,
                Timestamp = message.Timestamp,
                CorrelationId = message.CorrelationId
            };

            _recentActivities.Insert(0, activity);
            if (_recentActivities.Count > 50) _recentActivities.RemoveAt(_recentActivities.Count - 1);

            stats = new DashboardStatsDto
            {
                TotalEvents = _totalEvents,
                TotalRegistrations = _totalRegistrations,
                TotalCheckedIn = _totalCheckedIn,
                TotalRemainingSpots = _totalRemainingSpots,
                LastUpdated = DateTime.UtcNow,
                RecentActivities = _recentActivities.Take(15).ToList()
            };
        }

        await _hubContext.Clients.All.SendAsync("ReceiveDashboardStats", stats);
        await _hubContext.Clients.All.SendAsync("ReceiveEventActivity", activity);

        _logger.LogInformation("Broadcasted real-time Cancellation event: {Code} for {Event}", message.RegistrationCode, message.EventName);
    }

    public async Task HandleParticipantCheckedInAsync(ParticipantCheckedInMessage message)
    {
        DashboardStatsDto stats;
        ActivityLogDto activity;

        lock (_lock)
        {
            _totalCheckedIn++;

            activity = new ActivityLogDto
            {
                EventType = "ParticipantCheckedIn",
                Description = $"Người tham dự '{message.ParticipantName}' đã check-in thành công tại sự kiện (Mã: {message.RegistrationCode})",
                EventName = message.EventName,
                ParticipantName = message.ParticipantName,
                Timestamp = message.CheckedInAt,
                CorrelationId = message.CorrelationId
            };

            _recentActivities.Insert(0, activity);
            if (_recentActivities.Count > 50) _recentActivities.RemoveAt(_recentActivities.Count - 1);

            stats = new DashboardStatsDto
            {
                TotalEvents = _totalEvents,
                TotalRegistrations = _totalRegistrations,
                TotalCheckedIn = _totalCheckedIn,
                TotalRemainingSpots = _totalRemainingSpots,
                LastUpdated = DateTime.UtcNow,
                RecentActivities = _recentActivities.Take(15).ToList()
            };
        }

        await _hubContext.Clients.All.SendAsync("ReceiveDashboardStats", stats);
        await _hubContext.Clients.All.SendAsync("ReceiveEventActivity", activity);

        _logger.LogInformation("Broadcasted real-time Check-In event: {Code} for {Event}", message.RegistrationCode, message.EventName);
    }
}
