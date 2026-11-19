using MediQueue.Domain.Appointments;

namespace MediQueue.UnitTests.Appointments;

public sealed class AppointmentStatusTests
{
    [Fact]
    public void Appointment_statuses_match_the_approved_state_machine()
    {
        AppointmentStatus[] expected =
        [
            AppointmentStatus.PendingNumber,
            AppointmentStatus.Waiting,
            AppointmentStatus.Serving,
            AppointmentStatus.Completed,
            AppointmentStatus.Cancelled,
            AppointmentStatus.NoShow
        ];

        Assert.Equal(expected, Enum.GetValues<AppointmentStatus>());
    }
}
