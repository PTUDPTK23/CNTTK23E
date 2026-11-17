using MediQueue.Domain.Common;
using MediQueue.Domain.Patients;
using MediQueue.Domain.Queues;
using MediQueue.Domain.Scheduling;

namespace MediQueue.Domain.Appointments;

public sealed class Appointment
{
    private Appointment()
    {
    }

    public Appointment(
        Guid id,
        Guid timeSlotId,
        Guid patientId,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || timeSlotId == Guid.Empty || patientId == Guid.Empty || createdByUserId == Guid.Empty)
        {
            throw new DomainRuleException("Appointment identifiers are required.");
        }

        Id = id;
        TimeSlotId = timeSlotId;
        PatientId = patientId;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        UpdatedAtUtc = CreatedAtUtc;
        Status = AppointmentStatus.PendingNumber;
    }

    public Guid Id { get; private set; }

    public Guid TimeSlotId { get; private set; }

    public Guid PatientId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public AppointmentStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public DateTimeOffset? CheckedInAtUtc { get; private set; }

    public Guid? CheckedInByUserId { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public DateTimeOffset? NoShowAtUtc { get; private set; }

    public string? CancellationReason { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public TimeSlot TimeSlot { get; private set; } = null!;

    public Patient Patient { get; private set; } = null!;

    public QueueTicket? QueueTicket { get; private set; }

    public void MarkWaiting(DateTimeOffset changedAtUtc) =>
        Transition(AppointmentStatus.PendingNumber, AppointmentStatus.Waiting, changedAtUtc);

    public void StartServing(DateTimeOffset changedAtUtc) =>
        Transition(AppointmentStatus.Waiting, AppointmentStatus.Serving, changedAtUtc);

    public void Complete(DateTimeOffset changedAtUtc) =>
        Transition(AppointmentStatus.Serving, AppointmentStatus.Completed, changedAtUtc);

    public void MarkNoShow(DateTimeOffset changedAtUtc)
    {
        Transition(AppointmentStatus.Waiting, AppointmentStatus.NoShow, changedAtUtc);
        NoShowAtUtc = changedAtUtc.ToUniversalTime();
    }

    public void Cancel(DateTimeOffset changedAtUtc, DateTimeOffset slotStartAtUtc, string? reason = null)
    {
        if (Status is not AppointmentStatus.PendingNumber and not AppointmentStatus.Waiting)
        {
            throw new DomainRuleException($"Appointment in state {Status} cannot be cancelled.");
        }

        if (changedAtUtc >= slotStartAtUtc)
        {
            throw new DomainRuleException("Appointment cannot be cancelled at or after the time slot starts.");
        }

        Status = AppointmentStatus.Cancelled;
        CancelledAtUtc = changedAtUtc.ToUniversalTime();
        UpdatedAtUtc = CancelledAtUtc.Value;
        CancellationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public void CheckIn(Guid receptionistUserId, DateTimeOffset checkedInAtUtc)
    {
        if (receptionistUserId == Guid.Empty)
        {
            throw new DomainRuleException("Receptionist user id is required.");
        }

        if (CheckedInAtUtc.HasValue)
        {
            return;
        }

        CheckedInByUserId = receptionistUserId;
        CheckedInAtUtc = checkedInAtUtc.ToUniversalTime();
        UpdatedAtUtc = CheckedInAtUtc.Value;
    }

    private void Transition(AppointmentStatus expected, AppointmentStatus next, DateTimeOffset changedAtUtc)
    {
        if (Status != expected)
        {
            throw new DomainRuleException($"Appointment transition {Status} -> {next} is not allowed.");
        }

        Status = next;
        UpdatedAtUtc = changedAtUtc.ToUniversalTime();
    }
}
