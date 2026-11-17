using MediQueue.Domain.Appointments;
using MediQueue.Domain.Common;
using MediQueue.Domain.Departments;
using MediQueue.Domain.Queues;
using MediQueue.Domain.Scheduling;

namespace MediQueue.Domain.Doctors;

public sealed class Doctor
{
    private Doctor()
    {
    }

    public Doctor(Guid id, Guid userId, Guid departmentId, string displayName)
    {
        if (id == Guid.Empty || userId == Guid.Empty || departmentId == Guid.Empty)
        {
            throw new DomainRuleException("Doctor id, user id and department id are required.");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainRuleException("Doctor display name is required.");
        }

        Id = id;
        UserId = userId;
        DepartmentId = departmentId;
        DisplayName = displayName.Trim();
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public Department Department { get; private set; } = null!;

    public ICollection<DoctorSchedule> Schedules { get; } = new List<DoctorSchedule>();

    public ICollection<TimeSlot> TimeSlots { get; } = new List<TimeSlot>();

    public ICollection<DailyQueue> DailyQueues { get; } = new List<DailyQueue>();

    public ICollection<QueueTicket> QueueTickets { get; } = new List<QueueTicket>();
}
