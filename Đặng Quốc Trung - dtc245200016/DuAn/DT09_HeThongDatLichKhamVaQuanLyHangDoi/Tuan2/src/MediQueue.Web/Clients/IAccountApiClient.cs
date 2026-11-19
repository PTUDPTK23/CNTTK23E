using MediQueue.Contracts.Authentication;

namespace MediQueue.Web.Clients;

public interface IAccountApiClient
{
    Task<ApiClientResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<ApiClientResult<AccountProfileResponse>> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken);
    Task<ApiClientResult<AccountProfileResponse>> GetProfileAsync(string accessToken, CancellationToken cancellationToken);
    Task<ApiClientResult<AccountProfileResponse>> UpdateProfileAsync(string accessToken, UpdateProfileRequest request, CancellationToken cancellationToken);
    Task<ApiClientResult> ChangePasswordAsync(string accessToken, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task<ApiClientResult> LogoutAsync(string accessToken, CancellationToken cancellationToken);
}
