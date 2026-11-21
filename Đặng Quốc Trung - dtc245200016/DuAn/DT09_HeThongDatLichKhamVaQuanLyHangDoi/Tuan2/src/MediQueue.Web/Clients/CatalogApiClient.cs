using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MediQueue.Contracts.Catalogs;

namespace MediQueue.Web.Clients;

public sealed class CatalogApiClient(HttpClient http) : ICatalogApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public Task<ApiClientResult<PagedDepartmentsResponse>> GetDepartmentsAsync(string token, DepartmentListQuery q, CancellationToken ct) =>
        SendAsync<PagedDepartmentsResponse>(HttpMethod.Get, $"api/departments?search={Uri.EscapeDataString(q.Search ?? "")}&status={Uri.EscapeDataString(q.Status ?? "")}&sortBy={q.SortBy}&sortDirection={q.SortDirection}&page={q.Page}&pageSize={q.PageSize}", token, null, ct);
    public Task<ApiClientResult<DepartmentDetailsResponse>> GetDepartmentAsync(string token, Guid id, CancellationToken ct) => SendAsync<DepartmentDetailsResponse>(HttpMethod.Get, $"api/departments/{id}", token, null, ct);
    public Task<ApiClientResult<DepartmentDetailsResponse>> CreateDepartmentAsync(string token, CreateDepartmentRequest request, CancellationToken ct) => SendAsync<DepartmentDetailsResponse>(HttpMethod.Post, "api/departments", token, request, ct);
    public Task<ApiClientResult<DepartmentDetailsResponse>> UpdateDepartmentAsync(string token, Guid id, UpdateDepartmentRequest request, CancellationToken ct) => SendAsync<DepartmentDetailsResponse>(HttpMethod.Put, $"api/departments/{id}", token, request, ct);
    public Task<ApiClientResult> SetDepartmentActiveAsync(string token, Guid id, bool active, CancellationToken ct) => CommandAsync($"api/departments/{id}/{(active ? "activate" : "deactivate")}", token, ct);
    public Task<ApiClientResult<PagedRoomsResponse>> GetRoomsAsync(string token, RoomListQuery q, CancellationToken ct) =>
        SendAsync<PagedRoomsResponse>(HttpMethod.Get, $"api/rooms?search={Uri.EscapeDataString(q.Search ?? "")}&status={Uri.EscapeDataString(q.Status ?? "")}&sortBy={q.SortBy}&sortDirection={q.SortDirection}&page={q.Page}&pageSize={q.PageSize}", token, null, ct);
    public Task<ApiClientResult<RoomDetailsResponse>> GetRoomAsync(string token, Guid id, CancellationToken ct) => SendAsync<RoomDetailsResponse>(HttpMethod.Get, $"api/rooms/{id}", token, null, ct);
    public Task<ApiClientResult<RoomDetailsResponse>> CreateRoomAsync(string token, CreateRoomRequest request, CancellationToken ct) => SendAsync<RoomDetailsResponse>(HttpMethod.Post, "api/rooms", token, request, ct);
    public Task<ApiClientResult<RoomDetailsResponse>> UpdateRoomAsync(string token, Guid id, UpdateRoomRequest request, CancellationToken ct) => SendAsync<RoomDetailsResponse>(HttpMethod.Put, $"api/rooms/{id}", token, request, ct);
    public Task<ApiClientResult> SetRoomActiveAsync(string token, Guid id, bool active, CancellationToken ct) => CommandAsync($"api/rooms/{id}/{(active ? "activate" : "deactivate")}", token, ct);

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

    private async Task<ApiClientResult> CommandAsync(string url, string token, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using HttpResponseMessage response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            return response.IsSuccessStatusCode ? ApiClientResult.Success() : ApiClientResult.Failure(await ApiProblemReader.ReadAsync(response, ct));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested) { return ApiClientResult.Failure(ApiProblem.Unavailable()); }
    }
}
