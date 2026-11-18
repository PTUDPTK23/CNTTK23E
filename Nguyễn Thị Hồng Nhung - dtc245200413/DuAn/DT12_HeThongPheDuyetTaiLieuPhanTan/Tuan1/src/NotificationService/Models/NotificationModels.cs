namespace NotificationService.Models;

public class NotificationItem
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string RecipientRole { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ProcessedEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public record DocumentStatusChangedEvent(
    Guid EventId,
    string EventType,
    Guid DocumentId,
    string Title,
    string CreatedBy,
    string Approver,
    string FromStatus,
    string ToStatus,
    string Actor,
    string? Comment,
    string CorrelationId,
    DateTime OccurredAt
);
