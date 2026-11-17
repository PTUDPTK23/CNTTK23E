using System.Net;
using System.Net.Http.Json;
using MediQueue.Contracts.Health;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MediQueue.IntegrationTests.Web;

public sealed class MediQueueWebMvcTests : IClassFixture<MediQueueWebFactory>
{
    private readonly MediQueueWebFactory factory;

    public MediQueueWebMvcTests(MediQueueWebFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Home_page_is_rendered_by_mvc_with_layout_assets()
    {
        using HttpClient client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/");
        string html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h1 class=\"display-4\">MediQueue</h1>", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/lib/bootstrap/dist/css/bootstrap.min.css\"", html, StringComparison.Ordinal);
        Assert.Contains("src=\"/lib/bootstrap/dist/js/bootstrap.bundle.min.js\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/lib/bootstrap/dist/css/bootstrap.min.css")]
    [InlineData("/lib/bootstrap/dist/js/bootstrap.bundle.min.js")]
    [InlineData("/css/site.css")]
    [InlineData("/js/site.js")]
    [InlineData("/MediQueue.Web.styles.css")]
    public async Task Static_asset_is_available(string path)
    {
        using HttpClient client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Error_action_renders_the_mvc_error_view()
    {
        using HttpClient client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/Home/Error");
        string html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("An error occurred while processing your request.", html, StringComparison.Ordinal);
        Assert.Contains("Request ID:", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Health_endpoint_remains_available()
    {
        using HttpClient client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/health");
        ServiceStatusResponse? payload = await response.Content.ReadFromJsonAsync<ServiceStatusResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("MediQueue.Web", payload.Service);
        Assert.Equal("Healthy", payload.Status);
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}
