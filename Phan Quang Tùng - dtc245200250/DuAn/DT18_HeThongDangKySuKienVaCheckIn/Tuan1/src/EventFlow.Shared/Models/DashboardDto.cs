using System;
using System.Collections.Generic;

namespace EventFlow.Shared.Models;

public class DashboardStatsDto
{
    public int TotalEvents { get; set; }
    public int TotalRegistrations { get; set; }
    public int TotalCheckedIn { get; set; }
    public int TotalNotCheckedIn => Math.Max(0, TotalRegistrations - TotalCheckedIn);
    public int TotalRemainingSpots { get; set; }
    public double OverallCheckInRate => TotalRegistrations > 0 
        ? Math.Round((double)TotalCheckedIn / TotalRegistrations * 100, 1) 
        : 0.0;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<ActivityLogDto> RecentActivities { get; set; } = new();
}

public class ActivityLogDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;
}
