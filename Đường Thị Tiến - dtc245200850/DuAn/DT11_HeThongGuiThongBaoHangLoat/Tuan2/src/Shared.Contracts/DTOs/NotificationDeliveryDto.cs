using Shared.Contracts.Enums;

namespace Shared.Contracts.DTOs;

public class NotificationDeliveryDto
{
    public Guid JobId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid RecipientId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string? Destination { get; set; } // Email or Phone
    public DeliveryStatus Status { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}
