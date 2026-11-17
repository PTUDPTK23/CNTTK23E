using MediQueue.Domain.Common;

namespace MediQueue.Domain.Queues;

public sealed class QueueTransition
{
    private QueueTransition()
    {
    }

    public QueueTransition(
        Guid id,
        Guid queueTicketId,
        QueueTicketStatus fromStatus,
        QueueTicketStatus toStatus,
        Guid? performedByUserId,
        DateTimeOffset occurredAtUtc,
        string? reason = null)
    {
        if (id == Guid.Empty || queueTicketId == Guid.Empty)
        {
            throw new DomainRuleException("Queue transition identifiers are required.");
        }

        if (fromStatus == toStatus)
        {
            throw new DomainRuleException("Queue transition must change status.");
        }

        Id = id;
        QueueTicketId = queueTicketId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        PerformedByUserId = performedByUserId;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public Guid Id { get; private set; }

    public Guid QueueTicketId { get; private set; }

    public QueueTicketStatus FromStatus { get; private set; }

    public QueueTicketStatus ToStatus { get; private set; }

    public Guid? PerformedByUserId { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string? Reason { get; private set; }

    public QueueTicket QueueTicket { get; private set; } = null!;
}
