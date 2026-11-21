using System.ComponentModel.DataAnnotations;

namespace MediQueue.Contracts.Profiles;

public static class PatientManagementValues
{
    public static readonly string[] SortFields = ["name", "email", "status", "createdAt"];
    public static readonly int[] PageSizes = [10, 20, 50];
}

public sealed class PatientListQuery
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string SortBy { get; init; } = "name";
    public string SortDirection { get; init; } = "asc";
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public sealed record PatientSummaryResponse(int Total, int Active, int Inactive);
public sealed record PatientListItemResponse(
    Guid Id, Guid UserId, string FullName, string Email, string? PhoneNumber,
    DateOnly? DateOfBirth, bool ProfileIsActive, bool AccountIsActive, DateTimeOffset AccountCreatedAtUtc);
public sealed record PatientDetailsResponse(
    Guid Id, Guid UserId, string FullName, string Email, string? PhoneNumber,
    DateOnly? DateOfBirth, bool ProfileIsActive, bool AccountIsActive,
    DateTimeOffset AccountCreatedAtUtc, string RowVersion);
public sealed record PagedPatientsResponse(
    IReadOnlyList<PatientListItemResponse> Items, int Page, int PageSize,
    int TotalCount, int TotalPages, PatientSummaryResponse Summary);
public sealed record PatientSelfProfileResponse(bool HasProfile, PatientDetailsResponse? Profile);

public sealed class UpdatePatientProfileRequest
{
    [Required, StringLength(200, MinimumLength = 2)] public string FullName { get; init; } = string.Empty;
    [Phone, StringLength(32)] public string? PhoneNumber { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    [Required] public string RowVersion { get; init; } = string.Empty;
}
