namespace Shared.Contracts.DTOs;

public class NotificationJobMessage
{
    public Guid JobId { get; set; }
    public Guid CampaignId { get; set; }
    public Guid RecipientId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty; // Email or Phone number
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
}
