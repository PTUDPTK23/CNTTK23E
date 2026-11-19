using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MediQueue.Contracts.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace MediQueue.Web.Clients;

public sealed class AccountApiClient(HttpClient httpClient) : IAccountApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ApiClientResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken) => SendAsync<LoginResponse>(
            HttpMethod.Post,
            "api/account/login",
            accessToken: null,
            request,
            cancellationToken);

    public Task<ApiClientResult<AccountProfileResponse>> RegisterAsync(
        RegisterPatientRequest request,
        CancellationToken cancellationToken) => SendAsync<AccountProfileResponse>(
            HttpMethod.Post,
            "api/account/register",
            accessToken: null,
            request,
            cancellationToken);

    public Task<ApiClientResult<AccountProfileResponse>> GetProfileAsync(
        string accessToken,
        CancellationToken cancellationToken) => SendAsync<AccountProfileResponse>(
            HttpMethod.Get,
            "api/account/me",
            accessToken,
            body: null,
            cancellationToken);

    public Task<ApiClientResult<AccountProfileResponse>> UpdateProfileAsync(
        string accessToken,
        UpdateProfileRequest request,
        CancellationToken cancellationToken) => SendAsync<AccountProfileResponse>(
            HttpMethod.Put,
            "api/account/profile",
            accessToken,
            request,
            cancellationToken);

    public Task<ApiClientResult> ChangePasswordAsync(
        string accessToken,
        ChangePasswordRequest request,
        CancellationToken cancellationToken) => SendWithoutBodyAsync(
            HttpMethod.Post,
            "api/account/change-password",
            accessToken,
            request,
            cancellationToken);

    public Task<ApiClientResult> LogoutAsync(
        string accessToken,
        CancellationToken cancellationToken) => SendWithoutBodyAsync(
            HttpMethod.Post,
            "api/account/logout",
            accessToken,
            body: null,
            cancellationToken);

    private async Task<ApiClientResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        string? accessToken,
        object? body,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = CreateRequest(method, path, accessToken, body);
            using HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApiClientResult<T>.Failure(await ReadProblemAsync(response, cancellationToken));
            }

            T? value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return value is null
                ? ApiClientResult<T>.Failure(new ApiProblem(
                    HttpStatusCode.BadGateway,
                    "Phản hồi không hợp lệ",
                    "Dịch vụ trả về dữ liệu không đầy đủ.",
                    new Dictionary<string, string[]>()))
                : ApiClientResult<T>.Success(value);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException &&
            !cancellationToken.IsCancellationRequested)
        {
            return ApiClientResult<T>.Failure(ApiProblem.Unavailable());
        }
        catch (JsonException)
        {
            return ApiClientResult<T>.Failure(new ApiProblem(
                HttpStatusCode.BadGateway,
                "Phản hồi không hợp lệ",
                "Dịch vụ trả về dữ liệu không thể đọc được.",
                new Dictionary<string, string[]>()));
        }
    }

    private async Task<ApiClientResult> SendWithoutBodyAsync(
        HttpMethod method,
        string path,
        string accessToken,
        object? body,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = CreateRequest(method, path, accessToken, body);
            using HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            return response.IsSuccessStatusCode
                ? ApiClientResult.Success()
                : ApiClientResult.Failure(await ReadProblemAsync(response, cancellationToken));
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException &&
            !cancellationToken.IsCancellationRequested)
        {
            return ApiClientResult.Failure(ApiProblem.Unavailable());
        }
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string path,
        string? accessToken,
        object? body)
    {
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return request;
    }

    private static async Task<ApiProblem> ReadProblemAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            string title = GetString(root, "title") ?? "Yêu cầu chưa được xử lý";
            string detail = GetString(root, "detail") ?? "Vui lòng kiểm tra thông tin và thử lại.";
            var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            if (root.TryGetProperty("errors", out JsonElement validationErrors) &&
                validationErrors.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in validationErrors.EnumerateObject())
                {
                    errors[property.Name] = property.Value.ValueKind == JsonValueKind.Array
                        ? property.Value.EnumerateArray()
                            .Select(item => item.GetString())
                            .Where(value => !string.IsNullOrWhiteSpace(value))
                            .Cast<string>()
                            .ToArray()
                        : [];
                }
            }

            return new ApiProblem(response.StatusCode, title, detail, errors);
        }
        catch (JsonException)
        {
            return new ApiProblem(
                response.StatusCode,
                "Yêu cầu chưa được xử lý",
                "Dịch vụ không thể xử lý yêu cầu vào lúc này.",
                new Dictionary<string, string[]>());
        }
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}
