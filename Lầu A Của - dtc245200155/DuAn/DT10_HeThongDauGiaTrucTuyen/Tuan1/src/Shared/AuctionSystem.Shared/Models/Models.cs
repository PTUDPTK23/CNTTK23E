namespace AuctionSystem.Shared.Models;

public enum AuctionStatus
{
    Pending = 0,
    Active = 1,
    Ended = 2,
    Cancelled = 3
}

public class Auction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal StepPrice { get; set; }
    public string? CurrentWinner { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AuctionStatus Status { get; set; } = AuctionStatus.Pending;
    public string? Winner { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Bid> Bids { get; set; } = new();
}

public class Bid
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuctionId { get; set; }
    public string Bidder { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime BidTime { get; set; } = DateTime.UtcNow;
    public bool IsAccepted { get; set; } = true;
    public string? Reason { get; set; }
}

public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AuctionId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
