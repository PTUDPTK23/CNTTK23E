using System.Text.Json.Serialization;

namespace DocumentApi.Models;

public enum DocumentStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected
}

public class DocumentItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "Hồ sơ chung";
    public string CreatedBy { get; set; } = string.Empty;
    public string Approver { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<ApprovalHistory> Histories { get; set; } = new();
}

public class ApprovalHistory
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    [JsonIgnore]
    public DocumentItem? Document { get; set; }
    public string Action { get; set; } = string.Empty;
    public DocumentStatus? FromStatus { get; set; }
    public DocumentStatus ToStatus { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
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

public record CreateDocumentRequest(
    string Title,
    string Content,
    string? Category,
    string CreatedBy,
    string Approver
);

public record WorkflowActionRequest(
    string? Actor,
    string? Comment
);
