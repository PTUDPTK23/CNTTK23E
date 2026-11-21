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
        Update(code, name);
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public ICollection<Doctor> Doctors { get; } = new List<Doctor>();

    public void Update(string code, string name)
    {
        Code = RequireText(code, "Department code", 32).ToUpperInvariant();
        Name = RequireText(name, "Department name", 200);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string RequireText(string value, string fieldName, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException($"{fieldName} is required.");
        }

        string normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new DomainRuleException($"{fieldName} must not exceed {maximumLength} characters.");
        }

        return normalized;
    }
}
