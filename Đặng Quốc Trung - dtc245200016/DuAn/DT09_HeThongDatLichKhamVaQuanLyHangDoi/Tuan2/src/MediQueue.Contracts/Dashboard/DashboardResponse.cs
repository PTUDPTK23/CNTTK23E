namespace MediQueue.Contracts.Dashboard;

public sealed record DashboardResponse(
    string Role,
    DashboardAccountSummary Account,
    AdminDashboardResponse? Admin,
    DoctorDashboardResponse? Doctor,
    ReceptionistDashboardResponse? Receptionist,
    PatientDashboardResponse? Patient);

public sealed record DashboardAccountSummary(
    string DisplayName,
    string Email,
    string? PhoneNumber,
    bool IsActive);

public sealed record DashboardCountSummary(int Total, int Active)
{
    public int Inactive => Math.Max(0, Total - Active);
}

public sealed record RoleAccountCount(string Role, int Count);

public sealed record AdminDashboardResponse(
    DashboardCountSummary Accounts,
    DashboardCountSummary Doctors,
    DashboardCountSummary Patients,
    DashboardCountSummary Departments,
    DashboardCountSummary Rooms,
    IReadOnlyList<RoleAccountCount> AccountsByRole);

public sealed record DoctorDashboardResponse(
    bool HasProfile,
    Guid? DoctorId,
    string? DisplayName,
    string? DepartmentCode,
    string? DepartmentName,
    bool? IsActive);

public sealed record ReceptionistDashboardResponse(
    DashboardCountSummary Doctors,
    DashboardCountSummary Patients,
    DashboardCountSummary Departments,
    DashboardCountSummary Rooms);

public sealed record PatientDashboardResponse(
    bool HasProfile,
    Guid? PatientId,
    string? FullName,
    DateOnly? DateOfBirth,
    bool? IsActive);
