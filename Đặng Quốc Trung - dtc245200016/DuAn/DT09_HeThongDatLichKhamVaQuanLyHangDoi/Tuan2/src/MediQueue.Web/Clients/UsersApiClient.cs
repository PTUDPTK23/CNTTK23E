using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MediQueue.Contracts.Users;

namespace MediQueue.Web.Clients;

public sealed class UsersApiClient(HttpClient httpClient) : IUsersApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ApiClientResult<PagedUsersResponse>> GetUsersAsync(string token, UserListQuery query, CancellationToken ct)
    {
        string url = $"api/users?search={Uri.EscapeDataString(query.Search ?? string.Empty)}&role={Uri.EscapeDataString(query.Role ?? string.Empty)}&status={Uri.EscapeDataString(query.Status ?? string.Empty)}&lockout={Uri.EscapeDataString(query.Lockout ?? string.Empty)}&sortBy={query.SortBy}&sortDirection={query.SortDirection}&page={query.Page}&pageSize={query.PageSize}";
        return SendAsync<PagedUsersResponse>(HttpMethod.Get, url, token, null, ct);
    }

    public Task<ApiClientResult<UserDetailsResponse>> GetUserAsync(string token, Guid id, CancellationToken ct) =>
        SendAsync<UserDetailsResponse>(HttpMethod.Get, $"api/users/{id}", token, null, ct);
    public Task<ApiClientResult<UserDetailsResponse>> CreateAsync(string token, CreateStaffUserRequest request, CancellationToken ct) =>
        SendAsync<UserDetailsResponse>(HttpMethod.Post, "api/users", token, request, ct);
    public Task<ApiClientResult<UserDetailsResponse>> UpdateAsync(string token, Guid id, UpdateStaffUserRequest request, CancellationToken ct) =>
        SendAsync<UserDetailsResponse>(HttpMethod.Put, $"api/users/{id}", token, request, ct);
    public Task<ApiClientResult> DeactivateAsync(string token, Guid id, CancellationToken ct) => SendCommandAsync($"api/users/{id}/deactivate", token, ct);
    public Task<ApiClientResult> ActivateAsync(string token, Guid id, CancellationToken ct) => SendCommandAsync($"api/users/{id}/activate", token, ct);
    public Task<ApiClientResult> UnlockAsync(string token, Guid id, CancellationToken ct) => SendCommandAsync($"api/users/{id}/unlock", token, ct);

    private async Task<ApiClientResult<T>> SendAsync<T>(HttpMethod method, string url, string token, object? body, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null) request.Content = JsonContent.Create(body);
            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) return ApiClientResult<T>.Failure(await ApiProblemReader.ReadAsync(response, ct));
            T? value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            return value is null
                ? ApiClientResult<T>.Failure(ApiProblem.InvalidResponse())
                : ApiClientResult<T>.Success(value);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            return ApiClientResult<T>.Failure(ApiProblem.Unavailable());
        }
        catch (JsonException)
        {
            return ApiClientResult<T>.Failure(ApiProblem.InvalidResponse());
        }
    }

    private async Task<ApiClientResult> SendCommandAsync(string url, string token, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            return response.IsSuccessStatusCode ? ApiClientResult.Success() : ApiClientResult.Failure(await ApiProblemReader.ReadAsync(response, ct));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            return ApiClientResult.Failure(ApiProblem.Unavailable());
        }
    }
}
