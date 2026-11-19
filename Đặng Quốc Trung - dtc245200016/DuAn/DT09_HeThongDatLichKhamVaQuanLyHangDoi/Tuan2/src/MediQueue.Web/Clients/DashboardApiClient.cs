using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MediQueue.Contracts.Dashboard;

namespace MediQueue.Web.Clients;

public sealed class DashboardApiClient(HttpClient httpClient) : IDashboardApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ApiClientResult<DashboardResponse>> GetDashboardAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/dashboard/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApiClientResult<DashboardResponse>.Failure(
                    await ApiProblemReader.ReadAsync(response, cancellationToken));
            }

            DashboardResponse? value = await response.Content.ReadFromJsonAsync<DashboardResponse>(
                JsonOptions,
                cancellationToken);
            return value is null
                ? InvalidResponse("Dịch vụ trả về Dashboard không đầy đủ.")
                : ApiClientResult<DashboardResponse>.Success(value);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException &&
            !cancellationToken.IsCancellationRequested)
        {
            return ApiClientResult<DashboardResponse>.Failure(ApiProblem.Unavailable());
        }
        catch (JsonException)
        {
            return InvalidResponse("Dịch vụ trả về Dashboard không thể đọc được.");
        }
    }

    private static ApiClientResult<DashboardResponse> InvalidResponse(string detail) =>
        ApiClientResult<DashboardResponse>.Failure(new ApiProblem(
            HttpStatusCode.BadGateway,
            "Phản hồi không hợp lệ",
            detail,
            new Dictionary<string, string[]>()));
}
