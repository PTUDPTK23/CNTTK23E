using MediQueue.Contracts.Profiles;
using MediQueue.Web.Clients;

namespace MediQueue.IntegrationTests.Web;

public sealed class FakeProfilesApiClient : IProfilesApiClient
{
    public static readonly Guid DoctorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid PatientId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid DoctorUserId = Guid.Parse("33333333-3333-3333-3333-333333333334");
    private static readonly Guid PatientUserId = Guid.Parse("44444444-4444-4444-4444-444444444445");
    private static readonly Guid DepartmentId = Guid.Parse("33333333-3333-3333-3333-333333333335");
    private static DoctorDetailsResponse Doctor => new(DoctorId, DoctorUserId, "BS. Nguyễn An", DepartmentId, "TM", "Tim mạch", "doctor@mediqueue.test", "0901", true, true, DateTimeOffset.UtcNow, Convert.ToBase64String([1]));
    private static PatientDetailsResponse Patient => new(PatientId, PatientUserId, "Nguyễn An", "patient@mediqueue.test", "0902", new DateOnly(1998, 5, 20), true, true, DateTimeOffset.UtcNow, Convert.ToBase64String([1]));
    public Task<ApiClientResult<PagedDoctorsResponse>> GetDoctorsAsync(string t, DoctorListQuery q, CancellationToken c) => Task.FromResult(ApiClientResult<PagedDoctorsResponse>.Success(new([new(DoctorId, DoctorUserId, Doctor.DisplayName, DepartmentId, "TM", "Tim mạch", Doctor.Email, true, true, Doctor.AccountCreatedAtUtc)], 1, 10, 1, 1, new(1, 1, 0, 0), [new(DepartmentId, "TM", "Tim mạch")])));
    public Task<ApiClientResult<DoctorDetailsResponse>> GetDoctorAsync(string t, Guid id, CancellationToken c) => Task.FromResult(ApiClientResult<DoctorDetailsResponse>.Success(Doctor));
    public Task<ApiClientResult<DoctorCreateOptionsResponse>> GetDoctorCreateOptionsAsync(string t, CancellationToken c) => Task.FromResult(ApiClientResult<DoctorCreateOptionsResponse>.Success(new([new(DoctorUserId, "Nguyễn An", Doctor.Email, Doctor.PhoneNumber, DateTimeOffset.UtcNow)], [new(DepartmentId, "TM", "Tim mạch")])));
    public Task<ApiClientResult<DoctorDetailsResponse>> CreateDoctorAsync(string t, CreateDoctorProfileRequest r, CancellationToken c) => Task.FromResult(ApiClientResult<DoctorDetailsResponse>.Success(Doctor));
    public Task<ApiClientResult<DoctorDetailsResponse>> UpdateDoctorAsync(string t, Guid id, UpdateDoctorProfileRequest r, CancellationToken c) => Task.FromResult(ApiClientResult<DoctorDetailsResponse>.Success(Doctor));
    public Task<ApiClientResult<DoctorSelfProfileResponse>> GetDoctorSelfAsync(string t, CancellationToken c) => Task.FromResult(ApiClientResult<DoctorSelfProfileResponse>.Success(new(true, Doctor)));
    public Task<ApiClientResult<PagedPatientsResponse>> GetPatientsAsync(string t, PatientListQuery q, CancellationToken c) => Task.FromResult(ApiClientResult<PagedPatientsResponse>.Success(new([new(PatientId, PatientUserId, Patient.FullName, Patient.Email, Patient.PhoneNumber, Patient.DateOfBirth, true, true, Patient.AccountCreatedAtUtc)], 1, 10, 1, 1, new(1, 1, 0))));
    public Task<ApiClientResult<PatientDetailsResponse>> GetPatientAsync(string t, Guid id, CancellationToken c) => Task.FromResult(ApiClientResult<PatientDetailsResponse>.Success(Patient));
    public Task<ApiClientResult<PatientDetailsResponse>> UpdatePatientAsync(string t, Guid id, UpdatePatientProfileRequest r, CancellationToken c) => Task.FromResult(ApiClientResult<PatientDetailsResponse>.Success(Patient));
    public Task<ApiClientResult<PatientSelfProfileResponse>> GetPatientSelfAsync(string t, CancellationToken c) => Task.FromResult(ApiClientResult<PatientSelfProfileResponse>.Success(new(true, Patient)));
}
