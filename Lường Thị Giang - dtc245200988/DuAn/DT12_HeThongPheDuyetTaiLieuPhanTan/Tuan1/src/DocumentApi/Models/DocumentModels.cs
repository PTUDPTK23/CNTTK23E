using System.Text.Json.Serialization;

namespace DocumentApi.Models;

public class DocumentItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "Hồ sơ hành chính";
    public string Creator { get; set; } = string.Empty;
    public string Approver { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string? RejectionReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<ApprovalHistory> Histories { get; set; } = new();
}

public class ApprovalHistory
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }

    [JsonIgnore]
    public DocumentItem? Document { get; set; }
}

public record CreateDocumentRequest(
    string Title,
    string Content,
    string? Category,
    string Creator,
    string Approver
);

public record SubmitDocumentRequest(
    string? Actor,
    string? Comment
);

public record ApproveDocumentRequest(
    string? Actor,
    string? Comment
);

public record RejectDocumentRequest(
    string? Actor,
    string? Reason
);
