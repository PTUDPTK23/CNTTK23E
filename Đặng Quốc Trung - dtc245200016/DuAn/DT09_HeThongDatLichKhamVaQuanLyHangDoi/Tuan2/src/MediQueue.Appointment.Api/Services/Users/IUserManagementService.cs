using System.Security.Claims;
using MediQueue.Contracts.Users;

namespace MediQueue.Appointment.Api.Services.Users;

public interface IUserManagementService
{
    Task<AccountServiceResult<PagedUsersResponse>> GetUsersAsync(UserListQuery query, CancellationToken cancellationToken);
    Task<AccountServiceResult<UserDetailsResponse>> GetUserAsync(Guid id, CancellationToken cancellationToken);
    Task<AccountServiceResult<UserDetailsResponse>> CreateStaffAsync(ClaimsPrincipal actor, CreateStaffUserRequest request, CancellationToken cancellationToken);
    Task<AccountServiceResult<UserDetailsResponse>> UpdateStaffAsync(ClaimsPrincipal actor, Guid id, UpdateStaffUserRequest request, CancellationToken cancellationToken);
    Task<AccountServiceResult> SetActiveAsync(ClaimsPrincipal actor, Guid id, bool isActive, CancellationToken cancellationToken);
    Task<AccountServiceResult> UnlockAsync(ClaimsPrincipal actor, Guid id, CancellationToken cancellationToken);
}
