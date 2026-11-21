using MediQueue.Contracts.Users;

namespace MediQueue.Web.Clients;

public interface IUsersApiClient
{
    Task<ApiClientResult<PagedUsersResponse>> GetUsersAsync(string token, UserListQuery query, CancellationToken cancellationToken);
    Task<ApiClientResult<UserDetailsResponse>> GetUserAsync(string token, Guid id, CancellationToken cancellationToken);
    Task<ApiClientResult<UserDetailsResponse>> CreateAsync(string token, CreateStaffUserRequest request, CancellationToken cancellationToken);
    Task<ApiClientResult<UserDetailsResponse>> UpdateAsync(string token, Guid id, UpdateStaffUserRequest request, CancellationToken cancellationToken);
    Task<ApiClientResult> DeactivateAsync(string token, Guid id, CancellationToken cancellationToken);
    Task<ApiClientResult> ActivateAsync(string token, Guid id, CancellationToken cancellationToken);
    Task<ApiClientResult> UnlockAsync(string token, Guid id, CancellationToken cancellationToken);
}
