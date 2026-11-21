using System.ComponentModel.DataAnnotations;

namespace MediQueue.Contracts.Profiles;

public static class DoctorManagementValues
{
    public static readonly string[] SortFields = ["name", "department", "email", "status", "createdAt"];
    public static readonly int[] PageSizes = [10, 20, 50];
}

public sealed class DoctorListQuery
{
    public string? Search { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? Status { get; init; }
    public string SortBy { get; init; } = "name";
    public string SortDirection { get; init; } = "asc";
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public sealed record DoctorSummaryResponse(int Total, int Active, int Inactive, int UnlinkedDoctorAccounts);
public sealed record DoctorDepartmentOptionResponse(Guid Id, string Code, string Name);
public sealed record AvailableDoctorAccountResponse(Guid UserId, string DisplayName, string Email, string? PhoneNumber, DateTimeOffset CreatedAtUtc);
public sealed record DoctorCreateOptionsResponse(
    IReadOnlyList<AvailableDoctorAccountResponse> Accounts,
    IReadOnlyList<DoctorDepartmentOptionResponse> Departments);

public sealed record DoctorListItemResponse(
    Guid Id, Guid UserId, string DisplayName, Guid DepartmentId, string DepartmentCode,
    string DepartmentName, string Email, bool ProfileIsActive, bool AccountIsActive, DateTimeOffset AccountCreatedAtUtc);

public sealed record DoctorDetailsResponse(
    Guid Id, Guid UserId, string DisplayName, Guid DepartmentId, string DepartmentCode,
    string DepartmentName, string Email, string? PhoneNumber, bool ProfileIsActive,
    bool AccountIsActive, DateTimeOffset AccountCreatedAtUtc, string RowVersion);

public sealed record PagedDoctorsResponse(
    IReadOnlyList<DoctorListItemResponse> Items, int Page, int PageSize,
    int TotalCount, int TotalPages, DoctorSummaryResponse Summary,
    IReadOnlyList<DoctorDepartmentOptionResponse> Departments);

public sealed record DoctorSelfProfileResponse(bool HasProfile, DoctorDetailsResponse? Profile);

public sealed class CreateDoctorProfileRequest
{
    [Required] public Guid UserId { get; init; }
    [Required] public Guid DepartmentId { get; init; }
    [Required, StringLength(200, MinimumLength = 2)] public string DisplayName { get; init; } = string.Empty;
}

public sealed class UpdateDoctorProfileRequest
{
    [Required] public Guid DepartmentId { get; init; }
    [Required, StringLength(200, MinimumLength = 2)] public string DisplayName { get; init; } = string.Empty;
    [Required] public string RowVersion { get; init; } = string.Empty;
}
