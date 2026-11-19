namespace MediQueue.Infrastructure.Persistence.Entities;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public OutboxMessage(
        Guid id,
        string type,
        string payload,
        DateTimeOffset occurredAtUtc,
        Guid correlationId)
    {
        Id = id;
        Type = type;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        CorrelationId = correlationId;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string Payload { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public Guid CorrelationId { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public int RetryCount { get; private set; }

    public string? LastError { get; private set; }

    public void MarkPublished(DateTimeOffset publishedAtUtc)
    {
        PublishedAtUtc = publishedAtUtc.ToUniversalTime();
        LastError = null;
        NextAttemptAtUtc = null;
    }

    public void RecordFailure(string error, DateTimeOffset nextAttemptAtUtc)
    {
        RetryCount = checked(RetryCount + 1);
        LastError = error;
        NextAttemptAtUtc = nextAttemptAtUtc.ToUniversalTime();
    }
}
