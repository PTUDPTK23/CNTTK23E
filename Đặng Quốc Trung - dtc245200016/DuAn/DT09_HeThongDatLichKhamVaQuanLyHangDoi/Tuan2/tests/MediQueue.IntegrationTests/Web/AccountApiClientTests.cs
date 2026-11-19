using System.Net;
using System.Text;
using MediQueue.Contracts.Authentication;
using MediQueue.Web.Clients;

namespace MediQueue.IntegrationTests.Web;

public sealed class AccountApiClientTests
{
    [Fact]
    public async Task Profile_request_attaches_bearer_per_request_and_maps_success()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK,
            """{"userId":"8b7052ef-6f7f-4ae3-b67f-c96ac8df14fa","fullName":"Nguyễn An","email":"a@b.com","phoneNumber":null,"role":"Patient","patientId":null,"doctorId":null,"dateOfBirth":null}"""));
        var client = new AccountApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

        ApiClientResult<AccountProfileResponse> result = await client.GetProfileAsync("secret-token", default);

        Assert.True(result.Succeeded);
        Assert.Equal("Nguyễn An", result.Value!.FullName);
        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("secret-token", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Client_maps_validation_problem_without_retrying_command()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.BadRequest,
            """{"title":"Dữ liệu chưa hợp lệ","detail":"Kiểm tra lại.","errors":{"email":["Email đã tồn tại."]}}"""));
        var client = new AccountApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

        ApiClientResult<AccountProfileResponse> result = await client.RegisterAsync(
            new RegisterPatientRequest
            {
                FullName = "Nguyễn An",
                Email = "a@b.com",
                Password = "very-secure-password",
                ConfirmPassword = "very-secure-password"
            }, default);

        Assert.False(result.Succeeded);
        Assert.Equal(HttpStatusCode.BadRequest, result.Problem!.StatusCode);
        Assert.Equal("Email đã tồn tại.", result.Problem.ValidationErrors["email"].Single());
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Client_maps_network_failure_to_friendly_unavailable_problem()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("internal host must not leak"));
        var client = new AccountApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

        ApiClientResult<LoginResponse> result = await client.LoginAsync(
            new LoginRequest { Email = "a@b.com", Password = "password" }, default);

        Assert.True(result.Problem!.IsUnavailable);
        Assert.DoesNotContain("internal host", result.Problem.Detail, StringComparison.OrdinalIgnoreCase);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) => new(status)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastRequest = request;
            return Task.FromResult(responder(request));
        }
    }
}
