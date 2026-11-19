using MediQueue.Domain.Common;
using MediQueue.Domain.Scheduling;

namespace MediQueue.Domain.Rooms;

public sealed class Room
{
    private Room()
    {
    }

    public Room(Guid id, string code, string name, string? location = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainRuleException("Room id is required.");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Room code and name are required.");
        }

        Id = id;
        Code = code.Trim();
        Name = name.Trim();
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Location { get; private set; }

    public bool IsActive { get; private set; }

    public ICollection<DoctorSchedule> DoctorSchedules { get; } = new List<DoctorSchedule>();

    public ICollection<TimeSlot> TimeSlots { get; } = new List<TimeSlot>();
}
