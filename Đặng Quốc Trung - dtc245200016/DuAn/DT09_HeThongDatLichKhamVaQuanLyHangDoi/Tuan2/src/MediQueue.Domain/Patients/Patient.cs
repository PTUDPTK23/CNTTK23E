using MediQueue.Domain.Appointments;
using MediQueue.Domain.Common;

namespace MediQueue.Domain.Patients;

public sealed class Patient
{
    private Patient()
    {
    }

    public Patient(Guid id, Guid userId, string fullName, DateOnly? dateOfBirth = null)
    {
        if (id == Guid.Empty || userId == Guid.Empty)
        {
            throw new DomainRuleException("Patient id and user id are required.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainRuleException("Patient full name is required.");
        }

        Id = id;
        UserId = userId;
        FullName = fullName.Trim();
        DateOfBirth = dateOfBirth;
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public DateOnly? DateOfBirth { get; private set; }

    public string? PhoneNumber { get; private set; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public ICollection<Appointment> Appointments { get; } = new List<Appointment>();

    public void UpdateProfile(string fullName, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainRuleException("Patient full name is required.");
        }

        string normalizedFullName = fullName.Trim();
        if (normalizedFullName.Length > 200)
        {
            throw new DomainRuleException("Patient full name cannot exceed 200 characters.");
        }

        string? normalizedPhoneNumber = string.IsNullOrWhiteSpace(phoneNumber)
            ? null
            : phoneNumber.Trim();

        if (normalizedPhoneNumber?.Length > 32)
        {
            throw new DomainRuleException("Patient phone number cannot exceed 32 characters.");
        }

        FullName = normalizedFullName;
        PhoneNumber = normalizedPhoneNumber;
    }
}
