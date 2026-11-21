using MediQueue.Domain.Appointments;
using MediQueue.Domain.Common;

namespace MediQueue.UnitTests.Appointments;

public sealed class AppointmentInvariantTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 17, 1, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_appointment_starts_pending_number()
    {
        Appointment appointment = Create();

        Assert.Equal(AppointmentStatus.PendingNumber, appointment.Status);
    }

    [Fact]
    public void Appointment_rejects_transition_not_in_state_machine()
    {
        Appointment appointment = Create();

        Assert.Throws<DomainRuleException>(() => appointment.StartServing(CreatedAt.AddMinutes(1)));
    }

    [Fact]
    public void Appointment_follows_waiting_serving_completed_path()
    {
        Appointment appointment = Create();

        appointment.MarkWaiting(CreatedAt.AddMinutes(1));
        appointment.StartServing(CreatedAt.AddMinutes(2));
        appointment.Complete(CreatedAt.AddMinutes(3));

        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
    }

    [Fact]
    public void Appointment_cannot_be_cancelled_at_slot_start()
    {
        Appointment appointment = Create();
        DateTimeOffset slotStart = CreatedAt.AddDays(1);

        Assert.Throws<DomainRuleException>(() => appointment.Cancel(slotStart, slotStart));
    }

    private static Appointment Create() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CreatedAt);
}
