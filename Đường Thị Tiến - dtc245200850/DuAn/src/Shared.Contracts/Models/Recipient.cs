namespace Shared.Contracts.Models;

public class Recipient
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<NotificationDelivery> Deliveries { get; set; } = new List<NotificationDelivery>();
}
