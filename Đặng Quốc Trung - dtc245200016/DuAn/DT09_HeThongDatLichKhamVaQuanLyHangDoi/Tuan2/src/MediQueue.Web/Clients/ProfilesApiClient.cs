using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MediQueue.Contracts.Profiles;

namespace MediQueue.Web.Clients;

public sealed class ProfilesApiClient(HttpClient http) : IProfilesApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public Task<ApiClientResult<PagedDoctorsResponse>> GetDoctorsAsync(string token, DoctorListQuery q, CancellationToken ct) => SendAsync<PagedDoctorsResponse>(HttpMethod.Get,
        $"api/doctors?search={Uri.EscapeDataString(q.Search ?? "")}&departmentId={q.DepartmentId}&status={Uri.EscapeDataString(q.Status ?? "")}&sortBy={q.SortBy}&sortDirection={q.SortDirection}&page={q.Page}&pageSize={q.PageSize}", token, null, ct);
    public Task<ApiClientResult<DoctorDetailsResponse>> GetDoctorAsync(string token, Guid id, CancellationToken ct) => SendAsync<DoctorDetailsResponse>(HttpMethod.Get, $"api/doctors/{id}", token, null, ct);
    public Task<ApiClientResult<DoctorCreateOptionsResponse>> GetDoctorCreateOptionsAsync(string token, CancellationToken ct) => SendAsync<DoctorCreateOptionsResponse>(HttpMethod.Get, "api/doctors/create-options", token, null, ct);
    public Task<ApiClientResult<DoctorDetailsResponse>> CreateDoctorAsync(string token, CreateDoctorProfileRequest request, CancellationToken ct) => SendAsync<DoctorDetailsResponse>(HttpMethod.Post, "api/doctors", token, request, ct);
    public Task<ApiClientResult<DoctorDetailsResponse>> UpdateDoctorAsync(string token, Guid id, UpdateDoctorProfileRequest request, CancellationToken ct) => SendAsync<DoctorDetailsResponse>(HttpMethod.Put, $"api/doctors/{id}", token, request, ct);
    public Task<ApiClientResult<DoctorSelfProfileResponse>> GetDoctorSelfAsync(string token, CancellationToken ct) => SendAsync<DoctorSelfProfileResponse>(HttpMethod.Get, "api/doctors/me", token, null, ct);
    public Task<ApiClientResult<PagedPatientsResponse>> GetPatientsAsync(string token, PatientListQuery q, CancellationToken ct) => SendAsync<PagedPatientsResponse>(HttpMethod.Get,
        $"api/patients?search={Uri.EscapeDataString(q.Search ?? "")}&status={Uri.EscapeDataString(q.Status ?? "")}&sortBy={q.SortBy}&sortDirection={q.SortDirection}&page={q.Page}&pageSize={q.PageSize}", token, null, ct);
    public Task<ApiClientResult<PatientDetailsResponse>> GetPatientAsync(string token, Guid id, CancellationToken ct) => SendAsync<PatientDetailsResponse>(HttpMethod.Get, $"api/patients/{id}", token, null, ct);
    public Task<ApiClientResult<PatientDetailsResponse>> UpdatePatientAsync(string token, Guid id, UpdatePatientProfileRequest request, CancellationToken ct) => SendAsync<PatientDetailsResponse>(HttpMethod.Put, $"api/patients/{id}", token, request, ct);
    public Task<ApiClientResult<PatientSelfProfileResponse>> GetPatientSelfAsync(string token, CancellationToken ct) => SendAsync<PatientSelfProfileResponse>(HttpMethod.Get, "api/patients/me", token, null, ct);

    private async Task<ApiClientResult<T>> SendAsync<T>(HttpMethod method, string url, string token, object? body, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null) request.Content = JsonContent.Create(body);
            using HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) return ApiClientResult<T>.Failure(await ApiProblemReader.ReadAsync(response, ct));
            T? value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            return value is null ? ApiClientResult<T>.Failure(ApiProblem.InvalidResponse()) : ApiClientResult<T>.Success(value);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested) { return ApiClientResult<T>.Failure(ApiProblem.Unavailable()); }
        catch (JsonException) { return ApiClientResult<T>.Failure(ApiProblem.InvalidResponse()); }
    }
}
