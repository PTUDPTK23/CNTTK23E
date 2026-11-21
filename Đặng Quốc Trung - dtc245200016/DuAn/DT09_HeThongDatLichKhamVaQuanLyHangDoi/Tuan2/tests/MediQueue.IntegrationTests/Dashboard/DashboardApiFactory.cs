using System.Security.Claims;
using System.Text.Encodings.Web;
using MediQueue.Appointment.Api;
using MediQueue.Appointment.Api.Services.Dashboard;
using MediQueue.Contracts.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MediQueue.IntegrationTests.Dashboard;

public sealed class DashboardApiFactory : WebApplicationFactory<ApiApplicationMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:MediQueue", "Server=localhost;Database=MediQueueModelTests;Trusted_Connection=True;TrustServerCertificate=True;");
        builder.UseSetting("Jwt:Issuer", "MediQueue.Appointment.Api");
        builder.UseSetting("Jwt:Audience", "MediQueue.Web");
        builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(Enumerable.Range(1, 32).Select(value => (byte)value).ToArray()));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDashboardService>();
            services.AddSingleton<IDashboardService, FakeDashboardService>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = DashboardTestAuthenticationHandler.TestScheme;
                options.DefaultChallengeScheme = DashboardTestAuthenticationHandler.TestScheme;
                options.DefaultForbidScheme = DashboardTestAuthenticationHandler.TestScheme;
            }).AddScheme<AuthenticationSchemeOptions, DashboardTestAuthenticationHandler>(
                DashboardTestAuthenticationHandler.TestScheme,
                _ => { });
        });
    }
}

internal sealed class FakeDashboardService : IDashboardService
{
    public Task<DashboardResponse?> GetForUserAsync(Guid userId, string role, CancellationToken cancellationToken)
    {
        DashboardAccountSummary account = new("Test User", "test@mediqueue.test", null, true);
        DashboardResponse response = role switch
        {
            "Admin" => new(role, account, new AdminDashboardResponse(
                new DashboardCountSummary(4, 4), new DashboardCountSummary(1, 1),
                new DashboardCountSummary(1, 1), new DashboardCountSummary(0, 0),
                new DashboardCountSummary(0, 0),
                [new("Admin", 1), new("Doctor", 1), new("Receptionist", 1), new("Patient", 1)]), null, null, null),
            "Doctor" => new(role, account, null, new DoctorDashboardResponse(false, null, null, null, null, null), null, null),
            "Receptionist" => new(role, account, null, null, new ReceptionistDashboardResponse(
                new DashboardCountSummary(0, 0), new DashboardCountSummary(1, 1),
                new DashboardCountSummary(0, 0), new DashboardCountSummary(0, 0)), null),
            _ => new("Patient", account, null, null, null, new PatientDashboardResponse(false, null, null, null, null))
        };
        return Task.FromResult<DashboardResponse?>(response);
    }
}

internal sealed class DashboardTestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string TestScheme = "DashboardTest";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Role", out Microsoft.Extensions.Primitives.StringValues role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string subject = Request.Headers.TryGetValue("X-Test-UserId", out Microsoft.Extensions.Primitives.StringValues userId)
            ? userId.ToString()
            : Guid.Parse("432fb62b-2f97-4c33-b00e-e2739be091da").ToString();
        Claim[] claims = [new("sub", subject), new("role", role.ToString())];
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, TestScheme, ClaimTypes.Name, "role"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, TestScheme)));
    }
}
