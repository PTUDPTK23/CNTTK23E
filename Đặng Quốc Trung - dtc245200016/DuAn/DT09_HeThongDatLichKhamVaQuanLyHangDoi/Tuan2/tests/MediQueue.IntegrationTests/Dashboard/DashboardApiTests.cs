using System.Net;
using System.Net.Http.Json;
using MediQueue.Contracts.Dashboard;

namespace MediQueue.IntegrationTests.Dashboard;

public sealed class DashboardApiTests(DashboardApiFactory factory) : IClassFixture<DashboardApiFactory>
{
    [Fact]
    public async Task Anonymous_request_is_rejected()
    {
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/dashboard/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Doctor")]
    [InlineData("Receptionist")]
    [InlineData("Patient")]
    public async Task Valid_role_receives_only_its_dashboard_payload(string role)
    {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        using HttpResponseMessage response = await client.GetAsync("/api/dashboard/me");
        DashboardResponse? dashboard = await response.Content.ReadFromJsonAsync<DashboardResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dashboard);
        Assert.Equal(role, dashboard.Role);
        Assert.Equal(role == "Admin", dashboard.Admin is not null);
        Assert.Equal(role == "Doctor", dashboard.Doctor is not null);
        Assert.Equal(role == "Receptionist", dashboard.Receptionist is not null);
        Assert.Equal(role == "Patient", dashboard.Patient is not null);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString(), StringComparison.OrdinalIgnoreCase);
        string json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("accessToken", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_role_is_forbidden()
    {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Owner");
        using HttpResponseMessage response = await client.GetAsync("/api/dashboard/me");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Query_parameters_cannot_override_verified_identity_and_role()
    {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Patient");
        client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());
        using HttpResponseMessage response = await client.GetAsync(
            $"/api/dashboard/me?userId={Guid.NewGuid()}&role=Admin");
        DashboardResponse? dashboard = await response.Content.ReadFromJsonAsync<DashboardResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Patient", dashboard!.Role);
        Assert.Null(dashboard.Admin);
        Assert.NotNull(dashboard.Patient);
    }
}
