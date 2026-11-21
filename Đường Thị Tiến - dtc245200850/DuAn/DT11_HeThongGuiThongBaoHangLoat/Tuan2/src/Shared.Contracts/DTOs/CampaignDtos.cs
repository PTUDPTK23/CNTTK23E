using Shared.Contracts.Enums;

namespace Shared.Contracts.DTOs;

public class CreateCampaignRequest
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<Guid>? RecipientIds { get; set; }
    public bool SendImmediately { get; set; } = true;
}

public class CampaignSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public CampaignStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
    public int PendingCount { get; set; }
}

public class CampaignDetailDto : CampaignSummaryDto
{
    public string Content { get; set; } = string.Empty;
    public List<NotificationDeliveryDto> Deliveries { get; set; } = new();
}
