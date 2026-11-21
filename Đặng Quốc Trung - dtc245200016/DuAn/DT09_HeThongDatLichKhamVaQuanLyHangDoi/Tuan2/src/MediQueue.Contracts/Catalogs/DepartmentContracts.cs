using System.ComponentModel.DataAnnotations;

namespace MediQueue.Contracts.Catalogs;

public static class DepartmentManagementValues
{
    public static readonly string[] SortFields = ["code", "name", "status"];
    public static readonly int[] PageSizes = [10, 20, 50];
}

public sealed record DepartmentListQuery(
    string? Search = null, string? Status = null, string SortBy = "code",
    string SortDirection = "asc", int Page = 1, int PageSize = 10);

public sealed record DepartmentSummaryResponse(int Total, int Active, int Inactive);
public sealed record DepartmentListItemResponse(Guid Id, string Code, string Name, bool IsActive, int DoctorCount);
public sealed record DepartmentDetailsResponse(Guid Id, string Code, string Name, bool IsActive, int DoctorCount, int ActiveDoctorCount);
public sealed record PagedDepartmentsResponse(
    IReadOnlyList<DepartmentListItemResponse> Items, int Page, int PageSize,
    int TotalCount, int TotalPages, DepartmentSummaryResponse Summary);

public sealed record CreateDepartmentRequest(
    [Required, StringLength(32), RegularExpression("^[A-Za-z0-9_-]+$")] string Code,
    [Required, StringLength(200)] string Name);

public sealed record UpdateDepartmentRequest(
    [Required, StringLength(32), RegularExpression("^[A-Za-z0-9_-]+$")] string Code,
    [Required, StringLength(200)] string Name);
