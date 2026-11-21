namespace MediQueue.Infrastructure.Persistence.Entities;

public sealed class AuditLog
{
    private AuditLog()
    {
    }

    public AuditLog(
        Guid id,
        Guid? actorUserId,
        string action,
        string resourceType,
        string resourceId,
        DateTimeOffset occurredAtUtc,
        string? metadata = null)
    {
        Id = id;
        ActorUserId = actorUserId;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        Metadata = metadata;
    }

    public Guid Id { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string Action { get; private set; } = string.Empty;

    public string ResourceType { get; private set; } = string.Empty;

    public string ResourceId { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string? Metadata { get; private set; }
}
