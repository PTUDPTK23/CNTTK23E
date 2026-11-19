using MediQueue.Web;
using MediQueue.Web.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace MediQueue.IntegrationTests.Web;

public sealed class MediQueueWebFactory : WebApplicationFactory<WebApplicationMarker>
{
    public FakeAccountApiClient AccountApi { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("BackendApi:BaseAddress", "https://localhost:7101/");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccountApiClient>();
            services.AddSingleton<IAccountApiClient>(AccountApi);
            services.RemoveAll<IDataProtectionProvider>();
            services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        });
    }
}
