using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using MediQueue.Appointment.Api;
using MediQueue.Appointment.Api.Services;
using MediQueue.Appointment.Api.Services.Profiles;
using MediQueue.Contracts.Profiles;
using MediQueue.IntegrationTests.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MediQueue.IntegrationTests.Profiles;

public sealed class ProfileManagementApiTests : IClassFixture<ProfileManagementApiFactory>
{
    private readonly ProfileManagementApiFactory factory;
    public ProfileManagementApiTests(ProfileManagementApiFactory factory) => this.factory = factory;

    [Theory]
    [InlineData("/api/doctors")]
    [InlineData("/api/patients")]
    public async Task Management_endpoints_require_admin(string url)
    {
        using HttpClient anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(url)).StatusCode);
        using HttpClient receptionist = Client("Receptionist");
        Assert.Equal(HttpStatusCode.Forbidden, (await receptionist.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Admin_can_read_and_update_profiles_without_sensitive_fields()
    {
        using HttpClient admin = Client("Admin");
        string doctors = await admin.GetStringAsync("/api/doctors?search=an&sortBy=department&pageSize=10");
        string patients = await admin.GetStringAsync("/api/patients?status=active&pageSize=10");
        Assert.Contains("doctor@mediqueue.test", doctors);
        Assert.Contains("patient@mediqueue.test", patients);
        Assert.DoesNotContain("password", doctors, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", patients, StringComparison.OrdinalIgnoreCase);

        using HttpResponseMessage updated = await admin.PutAsJsonAsync($"/api/patients/{FakePatientManagementService.Id}",
            new UpdatePatientProfileRequest { FullName = "Nguyễn An", PhoneNumber = "0901234567", RowVersion = Convert.ToBase64String([1]) });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
    }

    [Fact]
    public async Task Self_endpoints_enforce_owner_role()
    {
        using HttpClient doctor = Client("Doctor");
        Assert.Equal(HttpStatusCode.OK, (await doctor.GetAsync("/api/doctors/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doctor.GetAsync("/api/patients/me")).StatusCode);
        using HttpClient patient = Client("Patient");
        Assert.Equal(HttpStatusCode.OK, (await patient.GetAsync("/api/patients/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await patient.GetAsync("/api/doctors/me")).StatusCode);
    }

    [Fact]
    public async Task Api_does_not_expose_delete_routes()
    {
        using HttpClient admin = Client("Admin");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await admin.DeleteAsync($"/api/doctors/{FakeDoctorManagementService.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await admin.DeleteAsync($"/api/patients/{FakePatientManagementService.Id}")).StatusCode);
    }

    private HttpClient Client(string role) { HttpClient client = factory.CreateClient(); client.DefaultRequestHeaders.Add("X-Test-Role", role); return client; }
}

public sealed class ProfileManagementApiFactory : WebApplicationFactory<ApiApplicationMarker>
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
            services.RemoveAll<IDoctorManagementService>(); services.AddSingleton<IDoctorManagementService, FakeDoctorManagementService>();
            services.RemoveAll<IPatientManagementService>(); services.AddSingleton<IPatientManagementService, FakePatientManagementService>();
            services.AddAuthentication(options => { options.DefaultAuthenticateScheme = DashboardTestAuthenticationHandler.TestScheme; options.DefaultChallengeScheme = DashboardTestAuthenticationHandler.TestScheme; options.DefaultForbidScheme = DashboardTestAuthenticationHandler.TestScheme; })
                .AddScheme<AuthenticationSchemeOptions, DashboardTestAuthenticationHandler>(DashboardTestAuthenticationHandler.TestScheme, _ => { });
        });
    }
}

internal sealed class FakeDoctorManagementService : IDoctorManagementService
{
    internal static readonly Guid Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid UserId = Guid.Parse("33333333-3333-3333-3333-333333333334");
    private static readonly Guid DepartmentId = Guid.Parse("33333333-3333-3333-3333-333333333335");
    private static DoctorDetailsResponse Item => new(Id, UserId, "BS. Nguyễn An", DepartmentId, "TM", "Tim mạch", "doctor@mediqueue.test", "0901", true, true, DateTimeOffset.UtcNow, Convert.ToBase64String([1]));
    public Task<AccountServiceResult<PagedDoctorsResponse>> GetAsync(DoctorListQuery q, CancellationToken t) => Task.FromResult(AccountServiceResult<PagedDoctorsResponse>.Success(new([new(Id, UserId, Item.DisplayName, DepartmentId, "TM", "Tim mạch", Item.Email, true, true, Item.AccountCreatedAtUtc)], 1, 10, 1, 1, new(1, 1, 0, 0), [new(DepartmentId, "TM", "Tim mạch")])));
    public Task<AccountServiceResult<DoctorDetailsResponse>> GetAsync(Guid id, CancellationToken t) => Task.FromResult(AccountServiceResult<DoctorDetailsResponse>.Success(Item));
    public Task<AccountServiceResult<DoctorCreateOptionsResponse>> GetCreateOptionsAsync(CancellationToken t) => Task.FromResult(AccountServiceResult<DoctorCreateOptionsResponse>.Success(new([], [new(DepartmentId, "TM", "Tim mạch")])));
    public Task<AccountServiceResult<DoctorDetailsResponse>> CreateAsync(ClaimsPrincipal a, CreateDoctorProfileRequest r, CancellationToken t) => Task.FromResult(AccountServiceResult<DoctorDetailsResponse>.Success(Item));
    public Task<AccountServiceResult<DoctorDetailsResponse>> UpdateAsync(ClaimsPrincipal a, Guid id, UpdateDoctorProfileRequest r, CancellationToken t) => Task.FromResult(AccountServiceResult<DoctorDetailsResponse>.Success(Item));
    public Task<AccountServiceResult<DoctorSelfProfileResponse>> GetSelfAsync(ClaimsPrincipal a, CancellationToken t) => Task.FromResult(AccountServiceResult<DoctorSelfProfileResponse>.Success(new(true, Item)));
}

internal sealed class FakePatientManagementService : IPatientManagementService
{
    internal static readonly Guid Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid UserId = Guid.Parse("44444444-4444-4444-4444-444444444445");
    private static PatientDetailsResponse Item => new(Id, UserId, "Nguyễn An", "patient@mediqueue.test", "0901", new DateOnly(1998, 5, 20), true, true, DateTimeOffset.UtcNow, Convert.ToBase64String([1]));
    public Task<AccountServiceResult<PagedPatientsResponse>> GetAsync(PatientListQuery q, CancellationToken t) => Task.FromResult(AccountServiceResult<PagedPatientsResponse>.Success(new([new(Id, UserId, Item.FullName, Item.Email, Item.PhoneNumber, Item.DateOfBirth, true, true, Item.AccountCreatedAtUtc)], 1, 10, 1, 1, new(1, 1, 0))));
    public Task<AccountServiceResult<PatientDetailsResponse>> GetAsync(Guid id, CancellationToken t) => Task.FromResult(AccountServiceResult<PatientDetailsResponse>.Success(Item));
    public Task<AccountServiceResult<PatientDetailsResponse>> UpdateAsync(ClaimsPrincipal a, Guid id, UpdatePatientProfileRequest r, CancellationToken t) => Task.FromResult(AccountServiceResult<PatientDetailsResponse>.Success(Item));
    public Task<AccountServiceResult<PatientSelfProfileResponse>> GetSelfAsync(ClaimsPrincipal a, CancellationToken t) => Task.FromResult(AccountServiceResult<PatientSelfProfileResponse>.Success(new(true, Item)));
}
