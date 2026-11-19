using System.Net;
using System.Text;
using MediQueue.Contracts.Dashboard;
using MediQueue.Web.Clients;

namespace MediQueue.IntegrationTests.Web;

public sealed class DashboardApiClientTests
{
    [Fact]
    public async Task Dashboard_request_attaches_bearer_per_request_and_maps_success()
    {
        const string json = """{"role":"Patient","account":{"displayName":"An","email":"an@example.test","phoneNumber":null,"isActive":true},"admin":null,"doctor":null,"receptionist":null,"patient":{"hasProfile":false,"patientId":null,"fullName":null,"dateOfBirth":null,"isActive":null}}""";
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, json));
        var client = new DashboardApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

        ApiClientResult<DashboardResponse> result = await client.GetDashboardAsync("secret-token", default);

        Assert.True(result.Succeeded);
        Assert.Equal("Patient", result.Value!.Role);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("secret-token", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Equal("/api/dashboard/me", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Dashboard_client_maps_problem_and_does_not_retry()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.Forbidden,
            """{"title":"Không có quyền","detail":"Vai trò không hợp lệ."}"""));
        var client = new DashboardApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

        ApiClientResult<DashboardResponse> result = await client.GetDashboardAsync("secret-token", default);

        Assert.False(result.Succeeded);
        Assert.Equal(HttpStatusCode.Forbidden, result.Problem!.StatusCode);
        Assert.Equal(1, handler.Calls);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastRequest = request;
            return Task.FromResult(responder(request));
        }
    }
}
