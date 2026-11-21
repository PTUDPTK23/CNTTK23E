using System.Security.Claims;
using MediQueue.Contracts.Authentication;

namespace MediQueue.Appointment.Api.Services;

public interface IAccountService
{
    Task<AccountServiceResult<AccountProfileResponse>> RegisterPatientAsync(
        RegisterPatientRequest request,
        CancellationToken cancellationToken);

    Task<AccountServiceResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    Task<AccountServiceResult> LogoutAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<AccountServiceResult<AccountProfileResponse>> GetProfileAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken);

    Task<AccountServiceResult<AccountProfileResponse>> UpdateProfileAsync(
        ClaimsPrincipal principal,
        UpdateProfileRequest request,
        CancellationToken cancellationToken);

    Task<AccountServiceResult> ChangePasswordAsync(
        ClaimsPrincipal principal,
        ChangePasswordRequest request,
        CancellationToken cancellationToken);
}
