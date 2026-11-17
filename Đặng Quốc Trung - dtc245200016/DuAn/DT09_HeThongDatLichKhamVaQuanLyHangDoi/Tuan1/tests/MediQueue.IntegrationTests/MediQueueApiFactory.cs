using MediQueue.Appointment.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MediQueue.IntegrationTests;

public sealed class MediQueueApiFactory : WebApplicationFactory<ApiApplicationMarker>
{
    private const string ModelOnlyConnectionString =
        "Server=localhost;Database=MediQueueModelTests;Trusted_Connection=True;TrustServerCertificate=True;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:MediQueue", ModelOnlyConnectionString);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MediQueue"] = ModelOnlyConnectionString
            });
        });
    }
}
