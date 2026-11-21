using System.ComponentModel.DataAnnotations;

namespace MediQueue.Contracts.Catalogs;

public static class RoomManagementValues
{
    public static readonly string[] SortFields = ["code", "name", "location", "status"];
    public static readonly int[] PageSizes = [10, 20, 50];
}

public sealed record RoomListQuery(
    string? Search = null, string? Status = null, string SortBy = "code",
    string SortDirection = "asc", int Page = 1, int PageSize = 10);

public sealed record RoomSummaryResponse(int Total, int Active, int Inactive);
public sealed record RoomListItemResponse(Guid Id, string Code, string Name, string? Location, bool IsActive, int ScheduleCount, int TimeSlotCount);
public sealed record RoomDetailsResponse(Guid Id, string Code, string Name, string? Location, bool IsActive, int ScheduleCount, int ActiveScheduleCount, int TimeSlotCount, int ActiveTimeSlotCount);
public sealed record PagedRoomsResponse(
    IReadOnlyList<RoomListItemResponse> Items, int Page, int PageSize,
    int TotalCount, int TotalPages, RoomSummaryResponse Summary);

public sealed record CreateRoomRequest(
    [Required, StringLength(32), RegularExpression("^[A-Za-z0-9_-]+$")] string Code,
    [Required, StringLength(200)] string Name,
    [StringLength(300)] string? Location);

public sealed record UpdateRoomRequest(
    [Required, StringLength(32), RegularExpression("^[A-Za-z0-9_-]+$")] string Code,
    [Required, StringLength(200)] string Name,
    [StringLength(300)] string? Location);
