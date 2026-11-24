namespace Shared;

public static class DocumentStatuses
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

public static class RabbitMqTopology
{
    public const string ExchangeName = "document.events.exchange";
    public const string QueueName = "document.events.queue";
    public const string DeadLetterExchangeName = "document.dlx";
    public const string DeadLetterQueueName = "document.events.dlq";
    public const string BindingRoutingKey = "document.*";
}

public sealed record DocumentStatusChangedEvent(
    Guid EventId,
    Guid DocumentId,
    string Title,
    string Category,
    string Creator,
    string Approver,
    string OldStatus,
    string NewStatus,
    string ActionBy,
    string? Comment,
    string CorrelationId,
    DateTime OccurredAtUtc
);
