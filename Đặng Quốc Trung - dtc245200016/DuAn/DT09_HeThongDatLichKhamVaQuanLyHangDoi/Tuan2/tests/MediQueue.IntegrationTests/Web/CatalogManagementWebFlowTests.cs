using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MediQueue.IntegrationTests.Web;

public sealed partial class CatalogManagementWebFlowTests : IClassFixture<MediQueueWebFactory>
{
    private readonly MediQueueWebFactory factory;
    public CatalogManagementWebFlowTests(MediQueueWebFactory factory) => this.factory = factory;

    [Theory]
    [InlineData("/Departments", "Quản lý chuyên khoa", "Tim mạch")]
    [InlineData("/Rooms", "Quản lý phòng khám", "Phòng khám 101")]
    public async Task Admin_can_open_catalog_pages_without_token_leak(string url, string heading, string item)
    {
        using HttpClient client = Client(); await Login(client, "admin@mediqueue.test");
        using HttpResponseMessage response = await client.GetAsync(url); string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains(heading, html); Assert.Contains(item, html); Assert.DoesNotContain(FakeAccountApiClient.SecretToken, html);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/css/catalog-management.css")).StatusCode);
    }

    [Theory]
    [InlineData("/Departments")]
    [InlineData("/Rooms")]
    public async Task Anonymous_redirects_and_patient_is_forbidden(string url)
    {
        using HttpClient anonymous = Client(); Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync(url)).StatusCode);
        using HttpClient patient = Client(); await Login(patient, "patient@mediqueue.test"); using HttpResponseMessage denied = await patient.GetAsync(url);
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode); Assert.Equal("/account/access-denied", denied.Headers.Location!.AbsolutePath);
    }

    private async Task Login(HttpClient client, string email)
    {
        string html = await (await client.GetAsync("/account/login")).Content.ReadAsStringAsync(); Match token = Token().Match(html);
        using HttpResponseMessage response = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string> { ["Email"] = email, ["Password"] = "a-valid-password", ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value) }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")] private static partial Regex Token();
}
