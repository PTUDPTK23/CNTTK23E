namespace AuctionSystem.Shared.Events;

public class BidAcceptedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public Guid AuctionId { get; set; }
    public string Bidder { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PreviousPrice { get; set; }
    public string CurrentWinner { get; set; } = string.Empty;
    public DateTime EndTime { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AuctionEndedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public Guid AuctionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Winner { get; set; }
    public decimal FinalPrice { get; set; }
    public int TotalBids { get; set; }
    public DateTime EndedAt { get; set; } = DateTime.UtcNow;
}

public class AuctionCreatedEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public Guid AuctionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal StepPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
