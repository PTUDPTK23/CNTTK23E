using MediQueue.Contracts.Catalogs;

namespace MediQueue.Web.Clients;

public interface ICatalogApiClient
{
    Task<ApiClientResult<PagedDepartmentsResponse>> GetDepartmentsAsync(string token, DepartmentListQuery query, CancellationToken ct);
    Task<ApiClientResult<DepartmentDetailsResponse>> GetDepartmentAsync(string token, Guid id, CancellationToken ct);
    Task<ApiClientResult<DepartmentDetailsResponse>> CreateDepartmentAsync(string token, CreateDepartmentRequest request, CancellationToken ct);
    Task<ApiClientResult<DepartmentDetailsResponse>> UpdateDepartmentAsync(string token, Guid id, UpdateDepartmentRequest request, CancellationToken ct);
    Task<ApiClientResult> SetDepartmentActiveAsync(string token, Guid id, bool active, CancellationToken ct);
    Task<ApiClientResult<PagedRoomsResponse>> GetRoomsAsync(string token, RoomListQuery query, CancellationToken ct);
    Task<ApiClientResult<RoomDetailsResponse>> GetRoomAsync(string token, Guid id, CancellationToken ct);
    Task<ApiClientResult<RoomDetailsResponse>> CreateRoomAsync(string token, CreateRoomRequest request, CancellationToken ct);
    Task<ApiClientResult<RoomDetailsResponse>> UpdateRoomAsync(string token, Guid id, UpdateRoomRequest request, CancellationToken ct);
    Task<ApiClientResult> SetRoomActiveAsync(string token, Guid id, bool active, CancellationToken ct);
}
