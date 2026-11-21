using Shared.Contracts.Enums;

namespace Shared.Contracts.Models;

public class NotificationDelivery
{
    public Guid JobId { get; set; } = Guid.NewGuid();

    public Guid CampaignId { get; set; }

    public Guid RecipientId { get; set; }

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;

    public int RetryCount { get; set; } = 0;

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    // Navigation properties
    public Campaign? Campaign { get; set; }

    public Recipient? Recipient { get; set; }
}
