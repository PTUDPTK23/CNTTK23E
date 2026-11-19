namespace MediQueue.Infrastructure.Persistence.Entities;

public sealed class IdempotencyRequest
{
    private IdempotencyRequest()
    {
    }

    public IdempotencyRequest(
        Guid userId,
        Guid idempotencyKey,
        string requestHash,
        int responseStatusCode,
        Guid? resourceId,
        DateTimeOffset createdAtUtc)
    {
        UserId = userId;
        IdempotencyKey = idempotencyKey;
        RequestHash = requestHash;
        ResponseStatusCode = responseStatusCode;
        ResourceId = resourceId;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
    }

    public Guid UserId { get; private set; }

    public Guid IdempotencyKey { get; private set; }

    public string RequestHash { get; private set; } = string.Empty;

    public int ResponseStatusCode { get; private set; }

    public Guid? ResourceId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
