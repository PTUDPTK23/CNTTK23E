using System.Net;
using System.Text;
using MediQueue.Contracts.Catalogs;
using MediQueue.Web.Clients;

namespace MediQueue.IntegrationTests.Web;

public sealed class CatalogApiClientTests
{
    [Fact]
    public async Task List_attaches_bearer_maps_payload_and_encodes_query()
    {
        const string json = """{"items":[],"page":1,"pageSize":10,"totalCount":0,"totalPages":0,"summary":{"total":0,"active":0,"inactive":0}}""";
        var handler = new StubHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, json)));
        var client = Client(handler);
        ApiClientResult<PagedDepartmentsResponse> result = await client.GetDepartmentsAsync("server-token", new(Search: "tim mạch"), default);
        Assert.True(result.Succeeded); Assert.Empty(result.Value!.Items);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("server-token", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Contains("search=tim%20m%E1%BA%A1ch", handler.LastRequest.RequestUri!.Query);
    }

    [Fact]
    public async Task Command_is_not_retried_when_backend_returns_conflict()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(Json(HttpStatusCode.Conflict, "{\"title\":\"Đang được sử dụng\",\"detail\":\"Còn dữ liệu liên kết.\"}")));
        var client = Client(handler);
        ApiClientResult result = await client.SetRoomActiveAsync("server-token", Guid.NewGuid(), false, default);
        Assert.False(result.Succeeded); Assert.Equal(HttpStatusCode.Conflict, result.Problem!.StatusCode); Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Network_failure_maps_safe_service_unavailable()
    {
        var handler = new StubHandler((_, _) => Task.FromException<HttpResponseMessage>(new HttpRequestException("private endpoint")));
        var client = Client(handler);
        ApiClientResult<PagedRoomsResponse> result = await client.GetRoomsAsync("server-token", new(), default);
        Assert.False(result.Succeeded); Assert.Equal(HttpStatusCode.ServiceUnavailable, result.Problem!.StatusCode);
        Assert.DoesNotContain("private endpoint", result.Problem.Detail, StringComparison.Ordinal);
    }

    private static CatalogApiClient Client(HttpMessageHandler handler) => new(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });
    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { Calls++; LastRequest = request; return responder(request, cancellationToken); }
    }
}
