using System.Security.Claims;
using MediQueue.Contracts.Catalogs;

namespace MediQueue.Appointment.Api.Services.Catalogs;

public interface IRoomService
{
    Task<AccountServiceResult<PagedRoomsResponse>> GetAsync(RoomListQuery query, CancellationToken token);
    Task<AccountServiceResult<RoomDetailsResponse>> GetAsync(Guid id, CancellationToken token);
    Task<AccountServiceResult<RoomDetailsResponse>> CreateAsync(ClaimsPrincipal actor, CreateRoomRequest request, CancellationToken token);
    Task<AccountServiceResult<RoomDetailsResponse>> UpdateAsync(ClaimsPrincipal actor, Guid id, UpdateRoomRequest request, CancellationToken token);
    Task<AccountServiceResult> SetActiveAsync(ClaimsPrincipal actor, Guid id, bool active, CancellationToken token);
}
