namespace MediQueue.Domain.Appointments;

public enum AppointmentStatus
{
    PendingNumber = 0,
    Waiting = 1,
    Serving = 2,
    Completed = 3,
    Cancelled = 4,
    NoShow = 5
}
