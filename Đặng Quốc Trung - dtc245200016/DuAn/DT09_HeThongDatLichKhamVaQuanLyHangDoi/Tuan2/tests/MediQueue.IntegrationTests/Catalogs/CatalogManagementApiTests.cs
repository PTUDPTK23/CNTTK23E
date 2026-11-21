using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using MediQueue.Appointment.Api;
using MediQueue.Appointment.Api.Services;
using MediQueue.Appointment.Api.Services.Catalogs;
using MediQueue.Contracts.Catalogs;
using MediQueue.IntegrationTests.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MediQueue.IntegrationTests.Catalogs;

public sealed class CatalogManagementApiTests : IClassFixture<CatalogManagementApiFactory>
{
    private readonly CatalogManagementApiFactory factory;
    public CatalogManagementApiTests(CatalogManagementApiFactory factory) => this.factory = factory;

    [Theory]
    [InlineData("/api/departments")]
    [InlineData("/api/rooms")]
    public async Task Catalogs_require_admin(string url)
    {
        using HttpClient anonymous = factory.CreateClient(); Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(url)).StatusCode);
        using HttpClient patient = factory.CreateClient(); patient.DefaultRequestHeaders.Add("X-Test-Role", "Patient"); Assert.Equal(HttpStatusCode.Forbidden, (await patient.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Admin_can_list_and_create_departments()
    {
        using HttpClient client = AdminClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/departments?page=1&pageSize=10")).StatusCode);
        using HttpResponseMessage invalid = await client.PostAsJsonAsync("/api/departments", new { code = "bad code", name = "" });
        Assert.True(invalid.StatusCode == HttpStatusCode.BadRequest, $"{invalid.StatusCode}: {await invalid.Content.ReadAsStringAsync()}");
        using HttpResponseMessage created = await client.PostAsJsonAsync("/api/departments", new CreateDepartmentRequest("TM", "Tim mạch"));
        Assert.True(created.StatusCode == HttpStatusCode.Created, $"{created.StatusCode}: {await created.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task Admin_can_list_create_and_change_room_state()
    {
        using HttpClient client = AdminClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/rooms?page=1&pageSize=10")).StatusCode);
        using HttpResponseMessage created = await client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("P101", "Phòng 101", "Tầng 1"));
        Assert.True(created.StatusCode == HttpStatusCode.Created, $"{created.StatusCode}: {await created.Content.ReadAsStringAsync()}");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/rooms/{FakeRoomService.Id}/deactivate", null)).StatusCode);
    }
    private HttpClient AdminClient() { HttpClient client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-Test-Role", "Admin"); return client; }
}

public sealed class CatalogManagementApiFactory : WebApplicationFactory<ApiApplicationMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.UseSetting("ConnectionStrings:MediQueue", "Server=localhost;Database=MediQueueModelTests;Trusted_Connection=True;TrustServerCertificate=True;");
        builder.UseSetting("Jwt:Issuer", "MediQueue.Appointment.Api"); builder.UseSetting("Jwt:Audience", "MediQueue.Web");
        builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(Enumerable.Range(1, 32).Select(x => (byte)x).ToArray()));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDepartmentService>(); services.AddSingleton<IDepartmentService, FakeDepartmentService>();
            services.RemoveAll<IRoomService>(); services.AddSingleton<IRoomService, FakeRoomService>();
            services.AddAuthentication(options => { options.DefaultAuthenticateScheme = DashboardTestAuthenticationHandler.TestScheme; options.DefaultChallengeScheme = DashboardTestAuthenticationHandler.TestScheme; options.DefaultForbidScheme = DashboardTestAuthenticationHandler.TestScheme; })
                .AddScheme<AuthenticationSchemeOptions, DashboardTestAuthenticationHandler>(DashboardTestAuthenticationHandler.TestScheme, _ => { });
        });
    }
}

internal sealed class FakeDepartmentService : IDepartmentService
{
    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static DepartmentDetailsResponse Item => new(Id, "TM", "Tim mạch", true, 0, 0);
    public Task<AccountServiceResult<PagedDepartmentsResponse>> GetAsync(DepartmentListQuery q, CancellationToken t) => Task.FromResult(AccountServiceResult<PagedDepartmentsResponse>.Success(new([new(Id, "TM", "Tim mạch", true, 0)], 1, 10, 1, 1, new(1, 1, 0))));
    public Task<AccountServiceResult<DepartmentDetailsResponse>> GetAsync(Guid id, CancellationToken t) => Task.FromResult(AccountServiceResult<DepartmentDetailsResponse>.Success(Item));
    public Task<AccountServiceResult<DepartmentDetailsResponse>> CreateAsync(ClaimsPrincipal a, CreateDepartmentRequest r, CancellationToken t) => Task.FromResult(AccountServiceResult<DepartmentDetailsResponse>.Success(Item));
    public Task<AccountServiceResult<DepartmentDetailsResponse>> UpdateAsync(ClaimsPrincipal a, Guid id, UpdateDepartmentRequest r, CancellationToken t) => Task.FromResult(AccountServiceResult<DepartmentDetailsResponse>.Success(Item));
    public Task<AccountServiceResult> SetActiveAsync(ClaimsPrincipal a, Guid id, bool active, CancellationToken t) => Task.FromResult(AccountServiceResult.Success());
}
internal sealed class FakeRoomService : IRoomService
{
    internal static readonly Guid Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static RoomDetailsResponse Item => new(Id, "P101", "Phòng 101", "Tầng 1", true, 0, 0, 0, 0);
    public Task<AccountServiceResult<PagedRoomsResponse>> GetAsync(RoomListQuery q, CancellationToken t) => Task.FromResult(AccountServiceResult<PagedRoomsResponse>.Success(new([new(Id, "P101", "Phòng 101", "Tầng 1", true, 0, 0)], 1, 10, 1, 1, new(1, 1, 0))));
    public Task<AccountServiceResult<RoomDetailsResponse>> GetAsync(Guid id, CancellationToken t) => Task.FromResult(AccountServiceResult<RoomDetailsResponse>.Success(Item));
    public Task<AccountServiceResult<RoomDetailsResponse>> CreateAsync(ClaimsPrincipal a, CreateRoomRequest r, CancellationToken t) => Task.FromResult(AccountServiceResult<RoomDetailsResponse>.Success(Item));
    public Task<AccountServiceResult<RoomDetailsResponse>> UpdateAsync(ClaimsPrincipal a, Guid id, UpdateRoomRequest r, CancellationToken t) => Task.FromResult(AccountServiceResult<RoomDetailsResponse>.Success(Item));
    public Task<AccountServiceResult> SetActiveAsync(ClaimsPrincipal a, Guid id, bool active, CancellationToken t) => Task.FromResult(AccountServiceResult.Success());
}
