using System.Security.Claims;
using MediQueue.Contracts.Profiles;

namespace MediQueue.Appointment.Api.Services.Profiles;

public interface IPatientManagementService
{
    Task<AccountServiceResult<PagedPatientsResponse>> GetAsync(PatientListQuery query, CancellationToken token);
    Task<AccountServiceResult<PatientDetailsResponse>> GetAsync(Guid id, CancellationToken token);
    Task<AccountServiceResult<PatientDetailsResponse>> UpdateAsync(ClaimsPrincipal actor, Guid id, UpdatePatientProfileRequest request, CancellationToken token);
    Task<AccountServiceResult<PatientSelfProfileResponse>> GetSelfAsync(ClaimsPrincipal actor, CancellationToken token);
}
