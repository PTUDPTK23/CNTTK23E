using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MediQueue.IntegrationTests.Web;

public sealed partial class ProfileManagementWebFlowTests : IClassFixture<MediQueueWebFactory>
{
    private readonly MediQueueWebFactory factory;
    public ProfileManagementWebFlowTests(MediQueueWebFactory factory) => this.factory = factory;

    [Theory]
    [InlineData("/Doctors", "Quản lý bác sĩ", "BS. Nguyễn An")]
    [InlineData("/Patients", "Quản lý bệnh nhân", "Nguyễn An")]
    public async Task Admin_can_open_profile_management_without_token_leak(string url, string heading, string item)
    {
        using HttpClient client = Client(); await Login(client, "admin@mediqueue.test");
        using HttpResponseMessage response = await client.GetAsync(url);
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(heading, html); Assert.Contains(item, html);
        Assert.DoesNotContain(FakeAccountApiClient.SecretToken, html);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/css/profile-management.css")).StatusCode);
    }

    [Fact]
    public async Task Doctor_can_open_own_professional_profile_only()
    {
        using HttpClient doctor = Client(); await Login(doctor, "doctor@mediqueue.test");
        Assert.Equal(HttpStatusCode.OK, (await doctor.GetAsync("/Doctors/Profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await doctor.GetAsync("/Doctors")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await doctor.GetAsync("/Patients")).StatusCode);
    }

    [Theory]
    [InlineData("/Doctors")]
    [InlineData("/Patients")]
    public async Task Anonymous_redirects_and_receptionist_is_forbidden(string url)
    {
        using HttpClient anonymous = Client(); Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync(url)).StatusCode);
        using HttpClient receptionist = Client(); await Login(receptionist, "receptionist@mediqueue.test");
        using HttpResponseMessage denied = await receptionist.GetAsync(url);
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
