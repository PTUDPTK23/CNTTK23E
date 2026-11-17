namespace MediQueue.Infrastructure.Persistence.Entities;

public sealed class InboxMessage
{
    private InboxMessage()
    {
    }

    public InboxMessage(Guid messageId, string consumerName, DateTimeOffset processedAtUtc, string? result = null)
    {
        MessageId = messageId;
        ConsumerName = consumerName;
        ProcessedAtUtc = processedAtUtc.ToUniversalTime();
        Result = result;
    }

    public Guid MessageId { get; private set; }

    public string ConsumerName { get; private set; } = string.Empty;

    public DateTimeOffset ProcessedAtUtc { get; private set; }

    public string? Result { get; private set; }
}
