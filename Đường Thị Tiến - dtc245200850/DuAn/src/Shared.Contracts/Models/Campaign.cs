using Shared.Contracts.Enums;

namespace Shared.Contracts.Models;

public class Campaign
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    // Navigation property
    public ICollection<NotificationDelivery> Deliveries { get; set; } = new List<NotificationDelivery>();
}
