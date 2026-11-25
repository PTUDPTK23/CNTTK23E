namespace AuctionSystem.Shared.Common;

public static class RabbitMqConstants
{
    public const string ExchangeName = "auction.exchange";
    public const string ExchangeType = "topic";

    // Routing keys
    public const string RoutingKeyBidAccepted = "auction.bid.accepted";
    public const string RoutingKeyAuctionEnded = "auction.ended";
    public const string RoutingKeyAuctionCreated = "auction.created";
    public const string RoutingKeyAllAuctionEvents = "auction.#";

    // Queue names
    public const string HubQueueName = "auction.hub.queue";
    public const string AuditQueueName = "auction.audit.queue";
    public const string DeadLetterQueueName = "auction.deadletter.queue";
    public const string DeadLetterExchangeName = "auction.deadletter.exchange";
}
