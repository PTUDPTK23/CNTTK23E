using System.Net;
using System.Text;
using MediQueue.Contracts.Users;
using MediQueue.Web.Clients;

namespace MediQueue.IntegrationTests.Web;

public sealed class UsersApiClientTests
{
    [Fact]
    public async Task List_request_attaches_bearer_and_maps_success_payload()
    {
        const string json = """{"items":[],"page":1,"pageSize":10,"totalCount":0,"totalPages":0,"summary":{"total":0,"active":0,"inactive":0,"locked":0,"doctors":0,"receptionists":0}}""";
        var handler = new StubHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, json)));
        var client = Client(handler);

        ApiClientResult<PagedUsersResponse> result = await client.GetUsersAsync(
            "secret-token",
            new UserListQuery(),
            default);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!.Items);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("secret-token", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Equal("/api/users", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Server_error_maps_problem_details_without_retry()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(Json(
            HttpStatusCode.InternalServerError,
            "{\"title\":\"Lỗi máy chủ\",\"detail\":\"Chi tiết nội bộ.\"}")));
        var client = Client(handler);

        ApiClientResult<PagedUsersResponse> result = await client.GetUsersAsync(
            "secret-token",
            new UserListQuery(),
            default);

        Assert.False(result.Succeeded);
        Assert.Equal(HttpStatusCode.InternalServerError, result.Problem!.StatusCode);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Network_failure_maps_service_unavailable()
    {
        var handler = new StubHandler((_, _) => Task.FromException<HttpResponseMessage>(new HttpRequestException("internal endpoint")));
        var client = Client(handler);

        ApiClientResult<PagedUsersResponse> result = await client.GetUsersAsync(
            "secret-token",
            new UserListQuery(),
            default);

        Assert.False(result.Succeeded);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.Problem!.StatusCode);
        Assert.True(result.Problem.IsUnavailable);
        Assert.DoesNotContain("internal endpoint", result.Problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Timeout_maps_service_unavailable()
    {
        var handler = new StubHandler((_, _) => Task.FromException<HttpResponseMessage>(new TaskCanceledException("timeout detail")));
        var client = Client(handler);

        ApiClientResult<PagedUsersResponse> result = await client.GetUsersAsync(
            "secret-token",
            new UserListQuery(),
            default);

        Assert.False(result.Succeeded);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.Problem!.StatusCode);
        Assert.DoesNotContain("timeout detail", result.Problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_success_payload_maps_bad_gateway()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, "{not-json")));
        var client = Client(handler);

        ApiClientResult<PagedUsersResponse> result = await client.GetUsersAsync(
            "secret-token",
            new UserListQuery(),
            default);

        Assert.False(result.Succeeded);
        Assert.Equal(HttpStatusCode.BadGateway, result.Problem!.StatusCode);
        Assert.Equal(1, handler.Calls);
    }

    private static UsersApiClient Client(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastRequest = request;
            return responder(request, cancellationToken);
        }
    }
}
