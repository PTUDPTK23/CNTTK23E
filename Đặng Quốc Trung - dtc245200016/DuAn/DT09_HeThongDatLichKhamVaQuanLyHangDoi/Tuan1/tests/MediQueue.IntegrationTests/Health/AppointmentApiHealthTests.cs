using System.Net;
using System.Net.Http.Json;
using MediQueue.Contracts.Health;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MediQueue.IntegrationTests.Health;

public sealed class AppointmentApiHealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public AppointmentApiHealthTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Health_endpoint_reports_the_api_as_healthy()
    {
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using HttpResponseMessage response = await client.GetAsync("/health");
        ServiceStatusResponse? payload = await response.Content.ReadFromJsonAsync<ServiceStatusResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("MediQueue.Appointment.Api", payload.Service);
        Assert.Equal("Healthy", payload.Status);
    }
}
