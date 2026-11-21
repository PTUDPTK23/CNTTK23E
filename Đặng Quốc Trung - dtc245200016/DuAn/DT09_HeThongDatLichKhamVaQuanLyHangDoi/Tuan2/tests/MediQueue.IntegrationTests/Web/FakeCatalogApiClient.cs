using MediQueue.Contracts.Catalogs;
using MediQueue.Web.Clients;

namespace MediQueue.IntegrationTests.Web;

public sealed class FakeCatalogApiClient : ICatalogApiClient
{
    public static readonly Guid DepartmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid RoomId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static DepartmentDetailsResponse Department => new(DepartmentId, "TM", "Tim mạch", true, 2, 1);
    private static RoomDetailsResponse Room => new(RoomId, "P101", "Phòng khám 101", "Tầng 1", true, 2, 1, 3, 2);
    public Task<ApiClientResult<PagedDepartmentsResponse>> GetDepartmentsAsync(string t, DepartmentListQuery q, CancellationToken c) => Task.FromResult(ApiClientResult<PagedDepartmentsResponse>.Success(new([new(DepartmentId, "TM", "Tim mạch", true, 2)], 1, 10, 1, 1, new(1, 1, 0))));
    public Task<ApiClientResult<DepartmentDetailsResponse>> GetDepartmentAsync(string t, Guid id, CancellationToken c) => Task.FromResult(ApiClientResult<DepartmentDetailsResponse>.Success(Department));
    public Task<ApiClientResult<DepartmentDetailsResponse>> CreateDepartmentAsync(string t, CreateDepartmentRequest r, CancellationToken c) => Task.FromResult(ApiClientResult<DepartmentDetailsResponse>.Success(Department));
    public Task<ApiClientResult<DepartmentDetailsResponse>> UpdateDepartmentAsync(string t, Guid id, UpdateDepartmentRequest r, CancellationToken c) => Task.FromResult(ApiClientResult<DepartmentDetailsResponse>.Success(Department));
    public Task<ApiClientResult> SetDepartmentActiveAsync(string t, Guid id, bool a, CancellationToken c) => Task.FromResult(ApiClientResult.Success());
    public Task<ApiClientResult<PagedRoomsResponse>> GetRoomsAsync(string t, RoomListQuery q, CancellationToken c) => Task.FromResult(ApiClientResult<PagedRoomsResponse>.Success(new([new(RoomId, "P101", "Phòng khám 101", "Tầng 1", true, 2, 3)], 1, 10, 1, 1, new(1, 1, 0))));
    public Task<ApiClientResult<RoomDetailsResponse>> GetRoomAsync(string t, Guid id, CancellationToken c) => Task.FromResult(ApiClientResult<RoomDetailsResponse>.Success(Room));
    public Task<ApiClientResult<RoomDetailsResponse>> CreateRoomAsync(string t, CreateRoomRequest r, CancellationToken c) => Task.FromResult(ApiClientResult<RoomDetailsResponse>.Success(Room));
    public Task<ApiClientResult<RoomDetailsResponse>> UpdateRoomAsync(string t, Guid id, UpdateRoomRequest r, CancellationToken c) => Task.FromResult(ApiClientResult<RoomDetailsResponse>.Success(Room));
    public Task<ApiClientResult> SetRoomActiveAsync(string t, Guid id, bool a, CancellationToken c) => Task.FromResult(ApiClientResult.Success());
}
