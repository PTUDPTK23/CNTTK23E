using System.Security.Claims;
using MediQueue.Contracts.Profiles;

namespace MediQueue.Appointment.Api.Services.Profiles;

public interface IDoctorManagementService
{
    Task<AccountServiceResult<PagedDoctorsResponse>> GetAsync(DoctorListQuery query, CancellationToken token);
    Task<AccountServiceResult<DoctorDetailsResponse>> GetAsync(Guid id, CancellationToken token);
    Task<AccountServiceResult<DoctorCreateOptionsResponse>> GetCreateOptionsAsync(CancellationToken token);
    Task<AccountServiceResult<DoctorDetailsResponse>> CreateAsync(ClaimsPrincipal actor, CreateDoctorProfileRequest request, CancellationToken token);
    Task<AccountServiceResult<DoctorDetailsResponse>> UpdateAsync(ClaimsPrincipal actor, Guid id, UpdateDoctorProfileRequest request, CancellationToken token);
    Task<AccountServiceResult<DoctorSelfProfileResponse>> GetSelfAsync(ClaimsPrincipal actor, CancellationToken token);
}
