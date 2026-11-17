using MediQueue.Domain.Appointments;
using MediQueue.Domain.Common;
using MediQueue.Domain.Doctors;

namespace MediQueue.Domain.Queues;

public sealed class QueueTicket
{
    private QueueTicket()
    {
    }

    public QueueTicket(
        Guid id,
        Guid dailyQueueId,
        Guid doctorId,
        DateOnly serviceDate,
        Guid appointmentId,
        int queueNumber,
        DateTimeOffset issuedAtUtc)
    {
        if (id == Guid.Empty || dailyQueueId == Guid.Empty || doctorId == Guid.Empty || appointmentId == Guid.Empty)
        {
            throw new DomainRuleException("Queue ticket identifiers are required.");
        }

        if (queueNumber <= 0)
        {
            throw new DomainRuleException("Queue number must be greater than zero.");
        }

        Id = id;
        DailyQueueId = dailyQueueId;
        DoctorId = doctorId;
        ServiceDate = serviceDate;
        AppointmentId = appointmentId;
        QueueNumber = queueNumber;
        IssuedAtUtc = issuedAtUtc.ToUniversalTime();
        Status = QueueTicketStatus.Waiting;
    }

    public Guid Id { get; private set; }

    public Guid DailyQueueId { get; private set; }

    public Guid DoctorId { get; private set; }

    public DateOnly ServiceDate { get; private set; }

    public Guid AppointmentId { get; private set; }

    public int QueueNumber { get; private set; }

    public QueueTicketStatus Status { get; private set; }

    public DateTimeOffset IssuedAtUtc { get; private set; }

    public DateTimeOffset? ServingAtUtc { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public DateTimeOffset? NoShowAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public DailyQueue DailyQueue { get; private set; } = null!;

    public Doctor Doctor { get; private set; } = null!;

    public Appointment Appointment { get; private set; } = null!;

    public ICollection<QueueTransition> Transitions { get; } = new List<QueueTransition>();

    public string DisplayNumber => QueueNumber.ToString("D3");

    public void StartServing(DateTimeOffset changedAtUtc)
    {
        Transition(QueueTicketStatus.Waiting, QueueTicketStatus.Serving);
        ServingAtUtc = changedAtUtc.ToUniversalTime();
    }

    public void Complete(DateTimeOffset changedAtUtc)
    {
        Transition(QueueTicketStatus.Serving, QueueTicketStatus.Completed);
        CompletedAtUtc = changedAtUtc.ToUniversalTime();
    }

    public void Cancel(DateTimeOffset changedAtUtc)
    {
        Transition(QueueTicketStatus.Waiting, QueueTicketStatus.Cancelled);
        CancelledAtUtc = changedAtUtc.ToUniversalTime();
    }

    public void MarkNoShow(DateTimeOffset changedAtUtc)
    {
        Transition(QueueTicketStatus.Waiting, QueueTicketStatus.NoShow);
        NoShowAtUtc = changedAtUtc.ToUniversalTime();
    }

    private void Transition(QueueTicketStatus expected, QueueTicketStatus next)
    {
        if (Status != expected)
        {
            throw new DomainRuleException($"Queue ticket transition {Status} -> {next} is not allowed.");
        }

        Status = next;
    }
}
