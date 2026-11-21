using System.Net;
using System.Text;
using MediQueue.Contracts.Profiles;
using MediQueue.Web.Clients;

namespace MediQueue.IntegrationTests.Web;

public sealed class ProfilesApiClientTests
{
    [Fact]
    public async Task Doctor_list_attaches_bearer_and_encodes_query()
    {
        const string json = """{"items":[],"page":1,"pageSize":10,"totalCount":0,"totalPages":0,"summary":{"total":0,"active":0,"inactive":0,"unlinkedDoctorAccounts":0},"departments":[]}""";
        var handler = new StubHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, json)));
        var client = new ProfilesApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

        ApiClientResult<PagedDoctorsResponse> result = await client.GetDoctorsAsync("server-token", new DoctorListQuery { Search = "bác sĩ", SortBy = "department" }, default);

        Assert.True(result.Succeeded); Assert.Empty(result.Value!.Items);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("server-token", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Contains("search=b%C3%A1c%20s%C4%A9", handler.LastRequest.RequestUri!.Query);
    }

    [Fact]
    public async Task Update_is_not_retried_and_maps_conflict()
    {
        var handler = new StubHandler((_, _) => Task.FromResult(Json(HttpStatusCode.Conflict, "{\"title\":\"Xung đột dữ liệu\",\"detail\":\"Vui lòng tải lại.\"}")));
        var client = new ProfilesApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

        ApiClientResult<PatientDetailsResponse> result = await client.UpdatePatientAsync("server-token", Guid.NewGuid(),
            new UpdatePatientProfileRequest { FullName = "Nguyễn An", RowVersion = Convert.ToBase64String([1]) }, default);

        Assert.False(result.Succeeded); Assert.Equal(HttpStatusCode.Conflict, result.Problem!.StatusCode); Assert.Equal(1, handler.Calls);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { Calls++; LastRequest = request; return responder(request, cancellationToken); }
    }
}
