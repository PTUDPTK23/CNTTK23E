using FileProcessing.Core.Enums;

namespace FileProcessing.Core.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string Payload { get; set; } = string.Empty;
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? LockedUntil { get; set; }
    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}

