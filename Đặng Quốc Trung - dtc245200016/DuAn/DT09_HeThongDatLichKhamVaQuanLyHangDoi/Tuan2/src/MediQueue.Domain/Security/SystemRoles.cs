namespace MediQueue.Domain.Security;

public static class SystemRoles
{
    public const string Patient = nameof(Patient);
    public const string Doctor = nameof(Doctor);
    public const string Receptionist = nameof(Receptionist);
    public const string Admin = nameof(Admin);

    public static IReadOnlyList<string> All { get; } =
        Array.AsReadOnly([Patient, Doctor, Receptionist, Admin]);
}
