using System.Net.Mail;

namespace Notification.Shared;

public sealed record CreateCampaign(string Name, string Subject, string Body, string[] Recipients);
public sealed record CampaignSummary(Guid Id, string Name, string Subject, string Body, string Status,
    DateTime CreatedAt, DateTime? StartedAt, long Total, long Pending, long Sent, long Failed);
public sealed record DeliveryView(Guid Id, string Address, string Status, int AttemptCount, int TotalAttempts,
    DateTime? NextAttemptAt, DateTime? SentAt, string? LastError);
public sealed record AttemptView(int Generation, string WorkerId, DateTime StartedAt, DateTime? FinishedAt,
    string Outcome, string? Error);
public sealed record SendNotification(Guid MessageId, Guid CampaignId, Guid DeliveryId, int Generation, int SchemaVersion = 1);
public sealed record ClaimedDelivery(Guid Id, Guid CampaignId, int Generation, Guid Owner, string Address,
    string Subject, string Body, int AttemptCount);
public sealed record DeliveryAuditEvent(Guid EventId, Guid CampaignId, Guid DeliveryId, int Generation,
    string Outcome, DateTime OccurredAt, int SchemaVersion = 1)
{
    public bool IsValid() => SchemaVersion == 1 && EventId != Guid.Empty && CampaignId != Guid.Empty &&
        DeliveryId != Guid.Empty && Generation > 0 && OccurredAt.Kind == DateTimeKind.Utc &&
        Outcome is "Sent" or "Failed" or "RetryScheduled" or "Interrupted";
}

public static class CampaignRules
{
    public const int MaxRecipients = 5000;
    public static (CreateCampaign? Campaign, string? Error) Validate(CreateCampaign? input)
    {
        if (input is null) return (null, "Thiếu dữ liệu chiến dịch.");
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Length > 120)
            return (null, "Tên phải có từ 1 đến 120 ký tự.");
        if (string.IsNullOrWhiteSpace(input.Subject) || input.Subject.Length > 200 || input.Subject.Contains('\r') || input.Subject.Contains('\n'))
            return (null, "Tiêu đề phải có từ 1 đến 200 ký tự và không chứa xuống dòng.");
        if (string.IsNullOrWhiteSpace(input.Body) || input.Body.Length > 20000)
            return (null, "Nội dung phải có từ 1 đến 20.000 ký tự.");
        if (input.Recipients is null || input.Recipients.Length is < 1 or > MaxRecipients)
            return (null, $"Danh sách cần từ 1 đến {MaxRecipients} địa chỉ.");
        var addresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in input.Recipients)
        {
            var value = raw?.Trim();
            if (string.IsNullOrEmpty(value) || value.Length > 254 || value.Contains('\r') || value.Contains('\n') ||
                !MailAddress.TryCreate(value, out var email) || email.Address != value || !email.Host.Contains('.'))
                return (null, "Danh sách có địa chỉ email không hợp lệ. Chỉ nhập địa chỉ, không kèm tên hiển thị.");
            // Quy ước của bài tập: địa chỉ được so sánh không phân biệt hoa/thường.
            addresses.Add(value.ToLowerInvariant());
        }
        return (input with { Name = input.Name.Trim(), Subject = input.Subject.Trim(), Recipients = addresses.ToArray() }, null);
    }

    public static bool IsPending(string status) => status is "Pending" or "Processing" or "RetryScheduled";
    public static TimeSpan RetryDelay(int attempt) => TimeSpan.FromSeconds(attempt <= 1 ? 10 : 30);
    public static bool ShouldRetry(bool permanent, int attempt, int maximum) => !permanent && attempt < maximum;
}
