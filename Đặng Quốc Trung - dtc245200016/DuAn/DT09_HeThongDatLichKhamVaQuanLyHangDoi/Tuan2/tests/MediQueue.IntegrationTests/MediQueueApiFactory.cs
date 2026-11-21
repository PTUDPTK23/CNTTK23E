using MediQueue.Appointment.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MediQueue.IntegrationTests;

public sealed class MediQueueApiFactory : WebApplicationFactory<ApiApplicationMarker>
{
    private const string ModelOnlyConnectionString =
        "Server=localhost;Database=MediQueueModelTests;Trusted_Connection=True;TrustServerCertificate=True;";
    private static readonly string TestSigningKey = Convert.ToBase64String(
        Enumerable.Range(1, 32).Select(number => (byte)number).ToArray());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:MediQueue", ModelOnlyConnectionString);
        builder.UseSetting("Jwt:Issuer", "MediQueue.Appointment.Api");
        builder.UseSetting("Jwt:Audience", "MediQueue.Web");
        builder.UseSetting("Jwt:AccessTokenLifetimeMinutes", "30");
        builder.UseSetting("Jwt:ClockSkewSeconds", "0");
        builder.UseSetting("Jwt:Algorithm", "HS256");
        builder.UseSetting("Jwt:SigningKey", TestSigningKey);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MediQueue"] = ModelOnlyConnectionString,
                ["Jwt:Issuer"] = "MediQueue.Appointment.Api",
                ["Jwt:Audience"] = "MediQueue.Web",
                ["Jwt:AccessTokenLifetimeMinutes"] = "30",
                ["Jwt:ClockSkewSeconds"] = "0",
                ["Jwt:Algorithm"] = "HS256",
                ["Jwt:SigningKey"] = TestSigningKey
            });
        });
    }
}
