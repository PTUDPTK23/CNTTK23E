using MediQueue.Domain.Appointments;
using MediQueue.Domain.Common;
using MediQueue.Domain.Doctors;
using MediQueue.Domain.Rooms;

namespace MediQueue.Domain.Scheduling;

public sealed class TimeSlot
{
    private TimeSlot()
    {
    }

    public TimeSlot(
        Guid id,
        Guid doctorScheduleId,
        Guid doctorId,
        Guid roomId,
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc,
        int capacity)
    {
        if (id == Guid.Empty || doctorScheduleId == Guid.Empty || doctorId == Guid.Empty || roomId == Guid.Empty)
        {
            throw new DomainRuleException("Time slot identifiers are required.");
        }

        if (endAtUtc <= startAtUtc)
        {
            throw new DomainRuleException("Time slot end must be after start.");
        }

        if (capacity <= 0)
        {
            throw new DomainRuleException("Time slot capacity must be greater than zero.");
        }

        Id = id;
        DoctorScheduleId = doctorScheduleId;
        DoctorId = doctorId;
        RoomId = roomId;
        StartAtUtc = startAtUtc.ToUniversalTime();
        EndAtUtc = endAtUtc.ToUniversalTime();
        Capacity = capacity;
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public Guid DoctorScheduleId { get; private set; }

    public Guid DoctorId { get; private set; }

    public Guid RoomId { get; private set; }

    public DateTimeOffset StartAtUtc { get; private set; }

    public DateTimeOffset EndAtUtc { get; private set; }

    public int Capacity { get; private set; }

    public int BookedCount { get; private set; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public DoctorSchedule DoctorSchedule { get; private set; } = null!;

    public Doctor Doctor { get; private set; } = null!;

    public Room Room { get; private set; } = null!;

    public ICollection<Appointment> Appointments { get; } = new List<Appointment>();

    public void ReservePlace()
    {
        if (!IsActive)
        {
            throw new DomainRuleException("Cannot reserve an inactive time slot.");
        }

        if (BookedCount >= Capacity)
        {
            throw new DomainRuleException("Time slot capacity is exhausted.");
        }

        BookedCount++;
    }

    public void ReleasePlace()
    {
        if (BookedCount <= 0)
        {
            throw new DomainRuleException("Booked count cannot be negative.");
        }

        BookedCount--;
    }
}
