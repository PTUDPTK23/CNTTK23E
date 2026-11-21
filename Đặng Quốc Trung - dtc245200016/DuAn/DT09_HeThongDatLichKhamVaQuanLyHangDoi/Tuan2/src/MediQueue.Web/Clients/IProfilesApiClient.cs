using MediQueue.Contracts.Profiles;

namespace MediQueue.Web.Clients;

public interface IProfilesApiClient
{
    Task<ApiClientResult<PagedDoctorsResponse>> GetDoctorsAsync(string token, DoctorListQuery query, CancellationToken ct);
    Task<ApiClientResult<DoctorDetailsResponse>> GetDoctorAsync(string token, Guid id, CancellationToken ct);
    Task<ApiClientResult<DoctorCreateOptionsResponse>> GetDoctorCreateOptionsAsync(string token, CancellationToken ct);
    Task<ApiClientResult<DoctorDetailsResponse>> CreateDoctorAsync(string token, CreateDoctorProfileRequest request, CancellationToken ct);
    Task<ApiClientResult<DoctorDetailsResponse>> UpdateDoctorAsync(string token, Guid id, UpdateDoctorProfileRequest request, CancellationToken ct);
    Task<ApiClientResult<DoctorSelfProfileResponse>> GetDoctorSelfAsync(string token, CancellationToken ct);
    Task<ApiClientResult<PagedPatientsResponse>> GetPatientsAsync(string token, PatientListQuery query, CancellationToken ct);
    Task<ApiClientResult<PatientDetailsResponse>> GetPatientAsync(string token, Guid id, CancellationToken ct);
    Task<ApiClientResult<PatientDetailsResponse>> UpdatePatientAsync(string token, Guid id, UpdatePatientProfileRequest request, CancellationToken ct);
    Task<ApiClientResult<PatientSelfProfileResponse>> GetPatientSelfAsync(string token, CancellationToken ct);
}
