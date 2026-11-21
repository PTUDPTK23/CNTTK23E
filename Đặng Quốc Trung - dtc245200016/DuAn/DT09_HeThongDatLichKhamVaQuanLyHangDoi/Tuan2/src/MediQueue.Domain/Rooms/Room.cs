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

        Id = id;
        Update(code, name, location);
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Location { get; private set; }

    public bool IsActive { get; private set; }

    public ICollection<DoctorSchedule> DoctorSchedules { get; } = new List<DoctorSchedule>();

    public ICollection<TimeSlot> TimeSlots { get; } = new List<TimeSlot>();

    public void Update(string code, string name, string? location)
    {
        Code = RequireText(code, "Room code", 32).ToUpperInvariant();
        Name = RequireText(name, "Room name", 200);
        Location = NormalizeOptional(location, 300, "Room location");
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string RequireText(string value, string fieldName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainRuleException($"{fieldName} is required.");
        string normalized = value.Trim();
        if (normalized.Length > maximumLength) throw new DomainRuleException($"{fieldName} must not exceed {maximumLength} characters.");
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string normalized = value.Trim();
        if (normalized.Length > maximumLength) throw new DomainRuleException($"{fieldName} must not exceed {maximumLength} characters.");
        return normalized;
    }
}
