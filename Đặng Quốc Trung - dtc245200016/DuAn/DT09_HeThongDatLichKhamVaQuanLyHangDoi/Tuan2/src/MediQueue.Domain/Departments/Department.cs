using MediQueue.Domain.Common;
using MediQueue.Domain.Doctors;

namespace MediQueue.Domain.Departments;

public sealed class Department
{
    private Department()
    {
    }

    public Department(Guid id, string code, string name)
    {
        if (id == Guid.Empty)
        {
            throw new DomainRuleException("Department id is required.");
        }

        Id = id;
        Code = RequireText(code, "Department code");
        Name = RequireText(name, "Department name");
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public ICollection<Doctor> Doctors { get; } = new List<Doctor>();

    public void Deactivate() => IsActive = false;

    private static string RequireText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException($"{fieldName} is required.");
        }

        return value.Trim();
    }
}
