using MediQueue.Domain.Common;
using MediQueue.Domain.Doctors;
using MediQueue.Domain.Rooms;

namespace MediQueue.Domain.Scheduling;

public sealed class DoctorSchedule
{
    private DoctorSchedule()
    {
    }

    public DoctorSchedule(
        Guid id,
        Guid doctorId,
        Guid roomId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        if (id == Guid.Empty || doctorId == Guid.Empty || roomId == Guid.Empty)
        {
            throw new DomainRuleException("Schedule id, doctor id and room id are required.");
        }

        if (endTime <= startTime)
        {
            throw new DomainRuleException("Schedule end time must be after start time.");
        }

        Id = id;
        DoctorId = doctorId;
        RoomId = roomId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public Guid DoctorId { get; private set; }

    public Guid RoomId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public bool IsActive { get; private set; }

    public Doctor Doctor { get; private set; } = null!;

    public Room Room { get; private set; } = null!;

    public ICollection<TimeSlot> TimeSlots { get; } = new List<TimeSlot>();
}
