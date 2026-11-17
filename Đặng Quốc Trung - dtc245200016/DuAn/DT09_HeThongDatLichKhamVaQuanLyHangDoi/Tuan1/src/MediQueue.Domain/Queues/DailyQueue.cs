using MediQueue.Domain.Common;
using MediQueue.Domain.Doctors;

namespace MediQueue.Domain.Queues;

public sealed class DailyQueue
{
    private DailyQueue()
    {
    }

    public DailyQueue(Guid id, Guid doctorId, DateOnly serviceDate)
    {
        if (id == Guid.Empty || doctorId == Guid.Empty)
        {
            throw new DomainRuleException("Daily queue id and doctor id are required.");
        }

        Id = id;
        DoctorId = doctorId;
        ServiceDate = serviceDate;
    }

    public Guid Id { get; private set; }

    public Guid DoctorId { get; private set; }

    public DateOnly ServiceDate { get; private set; }

    public int LastIssuedNumber { get; private set; }

    public Guid? CurrentTicketId { get; private set; }

    public long Version { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public Doctor Doctor { get; private set; } = null!;

    public QueueTicket? CurrentTicket { get; private set; }

    public ICollection<QueueTicket> Tickets { get; } = new List<QueueTicket>();

    public int IssueNextNumber()
    {
        LastIssuedNumber = checked(LastIssuedNumber + 1);
        IncrementVersion();
        return LastIssuedNumber;
    }

    public void SetCurrentTicket(Guid? ticketId)
    {
        if (ticketId == Guid.Empty)
        {
            throw new DomainRuleException("Current ticket id cannot be empty.");
        }

        CurrentTicketId = ticketId;
        IncrementVersion();
    }

    public void IncrementVersion() => Version = checked(Version + 1);
}
