using System.ComponentModel.DataAnnotations;

namespace MediQueue.Contracts.Users;

public static class UserManagementValues
{
    public static readonly string[] StaffRoles = ["Doctor", "Receptionist"];
    public static readonly string[] SortFields = ["name", "email", "role", "status", "createdAt"];
    public static readonly int[] PageSizes = [10, 20, 50];
}

public sealed class UserListQuery
{
    public string? Search { get; init; }
    public string? Role { get; init; }
    public string? Status { get; init; }
    public string? Lockout { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public string SortDirection { get; init; } = "desc";
    [Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public sealed record UserListItemResponse(
    Guid Id,
    string DisplayName,
    string Email,
    string Role,
    bool IsActive,
    bool IsLockedOut,
    DateTimeOffset CreatedAtUtc);

public sealed record UserDetailsResponse(
    Guid Id,
    string DisplayName,
    string Email,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    bool IsLockedOut,
    DateTimeOffset? LockoutEndUtc,
    DateTimeOffset CreatedAtUtc,
    bool HasDomainProfile);

public sealed record UserSummaryResponse(
    int Total,
    int Active,
    int Inactive,
    int LockedOut,
    int Doctors,
    int Receptionists);

public sealed record PagedUsersResponse(
    IReadOnlyList<UserListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    UserSummaryResponse Summary);

public sealed class CreateStaffUserRequest
{
    [Required, StringLength(150, MinimumLength = 2)] public string FullName { get; init; } = string.Empty;
    [Required, EmailAddress, StringLength(256)] public string Email { get; init; } = string.Empty;
    [Phone, StringLength(32)] public string? PhoneNumber { get; init; }
    [Required] public string Role { get; init; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 12)] public string Password { get; init; } = string.Empty;
}

public sealed class UpdateStaffUserRequest
{
    [Required, StringLength(150, MinimumLength = 2)] public string FullName { get; init; } = string.Empty;
    [Phone, StringLength(32)] public string? PhoneNumber { get; init; }
}
