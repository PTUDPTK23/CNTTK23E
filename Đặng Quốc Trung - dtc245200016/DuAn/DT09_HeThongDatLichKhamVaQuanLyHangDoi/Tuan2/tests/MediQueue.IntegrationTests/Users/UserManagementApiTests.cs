using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using MediQueue.Appointment.Api;
using MediQueue.Appointment.Api.Services;
using MediQueue.Appointment.Api.Services.Users;
using MediQueue.Contracts.Users;
using MediQueue.IntegrationTests.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MediQueue.IntegrationTests.Users;

public sealed class UserManagementApiTests : IClassFixture<UserManagementApiFactory>
{
    private readonly UserManagementApiFactory factory;
    public UserManagementApiTests(UserManagementApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Anonymous_is_rejected_and_patient_is_forbidden()
    {
        using HttpClient anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/users")).StatusCode);
        using HttpClient patient = factory.CreateClient(); patient.DefaultRequestHeaders.Add("X-Test-Role", "Patient");
        Assert.Equal(HttpStatusCode.Forbidden, (await patient.GetAsync("/api/users")).StatusCode);
    }

    [Fact]
    public async Task Admin_can_list_without_sensitive_identity_fields()
    {
        using HttpClient client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        using HttpResponseMessage response = await client.GetAsync("/api/users?search=an&page=1&pageSize=10");
        string json = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Bác sĩ An", json);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_can_create_only_through_validated_contract()
    {
        using HttpClient client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        using HttpResponseMessage invalid = await client.PostAsJsonAsync("/api/users", new { fullName = "A", email = "bad", role = "Admin", password = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using HttpResponseMessage valid = await client.PostAsJsonAsync("/api/users", new CreateStaffUserRequest { FullName = "Bác sĩ An", Email = "doctor@example.test", Role = "Doctor", Password = "valid-password-123" });
        Assert.Equal(HttpStatusCode.Created, valid.StatusCode);
    }
}

public sealed class UserManagementApiFactory : WebApplicationFactory<ApiApplicationMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:MediQueue", "Server=localhost;Database=MediQueueModelTests;Trusted_Connection=True;TrustServerCertificate=True;");
        builder.UseSetting("Jwt:Issuer", "MediQueue.Appointment.Api"); builder.UseSetting("Jwt:Audience", "MediQueue.Web");
        builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(Enumerable.Range(1, 32).Select(x => (byte)x).ToArray()));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserManagementService>(); services.AddSingleton<IUserManagementService, FakeUserManagementService>();
            services.AddAuthentication(options => { options.DefaultAuthenticateScheme = DashboardTestAuthenticationHandler.TestScheme; options.DefaultChallengeScheme = DashboardTestAuthenticationHandler.TestScheme; options.DefaultForbidScheme = DashboardTestAuthenticationHandler.TestScheme; })
                .AddScheme<AuthenticationSchemeOptions, DashboardTestAuthenticationHandler>(DashboardTestAuthenticationHandler.TestScheme, _ => { });
        });
    }
}

internal sealed class FakeUserManagementService : IUserManagementService
{
    private static readonly Guid Id = Guid.Parse("bc695646-6766-4149-9d90-0a37d7cc4bee");
    private static UserDetailsResponse User => new(Id, "Bác sĩ An", "doctor@example.test", null, "Doctor", true, false, null, DateTimeOffset.UtcNow, false);
    public Task<AccountServiceResult<PagedUsersResponse>> GetUsersAsync(UserListQuery q, CancellationToken c) => Task.FromResult(AccountServiceResult<PagedUsersResponse>.Success(new([new(Id, "Bác sĩ An", "doctor@example.test", "Doctor", true, false, DateTimeOffset.UtcNow)], 1, 10, 1, 1, new(1, 1, 0, 0, 1, 0))));
    public Task<AccountServiceResult<UserDetailsResponse>> GetUserAsync(Guid id, CancellationToken c) => Task.FromResult(AccountServiceResult<UserDetailsResponse>.Success(User));
    public Task<AccountServiceResult<UserDetailsResponse>> CreateStaffAsync(ClaimsPrincipal a, CreateStaffUserRequest r, CancellationToken c) => Task.FromResult(AccountServiceResult<UserDetailsResponse>.Success(User));
    public Task<AccountServiceResult<UserDetailsResponse>> UpdateStaffAsync(ClaimsPrincipal a, Guid id, UpdateStaffUserRequest r, CancellationToken c) => Task.FromResult(AccountServiceResult<UserDetailsResponse>.Success(User));
    public Task<AccountServiceResult> SetActiveAsync(ClaimsPrincipal a, Guid id, bool active, CancellationToken c) => Task.FromResult(AccountServiceResult.Success());
    public Task<AccountServiceResult> UnlockAsync(ClaimsPrincipal a, Guid id, CancellationToken c) => Task.FromResult(AccountServiceResult.Success());
}
