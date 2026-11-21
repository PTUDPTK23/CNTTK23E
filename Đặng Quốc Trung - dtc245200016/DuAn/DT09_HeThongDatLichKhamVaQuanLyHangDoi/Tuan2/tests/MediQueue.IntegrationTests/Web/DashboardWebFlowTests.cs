using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MediQueue.IntegrationTests.Web;

public sealed partial class DashboardWebFlowTests : IClassFixture<MediQueueWebFactory>
{
    private readonly MediQueueWebFactory factory;

    public DashboardWebFlowTests(MediQueueWebFactory factory) => this.factory = factory;

    [Fact]
    public async Task Anonymous_dashboard_redirects_to_shared_login()
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login", response.Headers.Location!.AbsolutePath);
        Assert.Contains("returnUrl=%2Fdashboard", response.Headers.Location.Query, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("admin@mediqueue.test", "Quản trị hệ thống", "Tổng quan quản trị")]
    [InlineData("doctor@mediqueue.test", "Không gian bác sĩ", "Tổng quan bác sĩ")]
    [InlineData("receptionist@mediqueue.test", "Không gian lễ tân", "Tổng quan lễ tân")]
    [InlineData("patient@mediqueue.test", "Không gian bệnh nhân", "Tổng quan bệnh nhân")]
    public async Task Login_redirects_to_role_dashboard_with_shared_workspace(
        string email,
        string expectedContent,
        string expectedTitle)
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage login = await LoginAsync(client, email);
        Assert.Equal("/dashboard", login.Headers.Location!.OriginalString);

        using HttpResponseMessage dashboard = await client.GetAsync("/dashboard");
        string html = WebUtility.HtmlDecode(await dashboard.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        Assert.Contains(expectedContent, html, StringComparison.Ordinal);
        Assert.Contains(expectedTitle, html, StringComparison.Ordinal);
        Assert.Contains("data-dashboard-shell", html, StringComparison.Ordinal);
        Assert.Contains("sidebar-preference-collapsed", html, StringComparison.Ordinal);
        Assert.Contains("/fonts/BeVietnamPro-Regular", html, StringComparison.Ordinal);
        Assert.Contains("/fonts/BeVietnamPro-Medium", html, StringComparison.Ordinal);
        Assert.Contains("/fonts/BeVietnamPro-Bold", html, StringComparison.Ordinal);
        Assert.DoesNotContain("dashboard-main page-enter", html, StringComparison.Ordinal);
        Assert.Contains("Hồ sơ cá nhân", html, StringComparison.Ordinal);
        Assert.Contains("Đổi mật khẩu", html, StringComparison.Ordinal);
        Assert.DoesNotContain(FakeAccountApiClient.SecretToken, html, StringComparison.Ordinal);
        Assert.DoesNotContain("Quản lý lịch hẹn", html, StringComparison.Ordinal);
        Assert.DoesNotContain("giai đoạn", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MediQueue Workspace", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/css/dashboard-layout.css")]
    [InlineData("/css/dashboard.css")]
    [InlineData("/js/dashboard.js")]
    [InlineData("/fonts/BeVietnamPro-Regular.ttf")]
    [InlineData("/fonts/BeVietnamPro-Medium.ttf")]
    [InlineData("/fonts/BeVietnamPro-Bold.ttf")]
    public async Task Dashboard_asset_is_available(string path)
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("admin@mediqueue.test", "/dashboard", "Tổng quan")]
    [InlineData("admin@mediqueue.test", "/Users", "Quản lý người dùng")]
    [InlineData("admin@mediqueue.test", "/Departments", "Quản lý chuyên khoa")]
    [InlineData("admin@mediqueue.test", "/Rooms", "Quản lý phòng khám")]
    [InlineData("admin@mediqueue.test", "/Doctors", "Quản lý bác sĩ")]
    [InlineData("admin@mediqueue.test", "/Patients", "Quản lý bệnh nhân")]
    [InlineData("doctor@mediqueue.test", "/Doctors/Profile", "Hồ sơ chuyên môn")]
    [InlineData("receptionist@mediqueue.test", "/Account/Profile", "Hồ sơ cá nhân")]
    [InlineData("patient@mediqueue.test", "/Account/Profile", "Hồ sơ cá nhân")]
    public async Task Sidebar_marks_exactly_one_current_navigation_item(
        string email,
        string path,
        string expectedLabel)
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage login = await LoginAsync(client, email);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        using HttpResponseMessage response = await client.GetAsync(path);
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        MatchCollection activeLinks = ActiveSidebarLinkRegex().Matches(html);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Match activeLink = Assert.Single(activeLinks.Cast<Match>());
        Assert.Equal(expectedLabel, activeLink.Groups[1].Value);
    }

    [Theory]
    [InlineData("dashboard401@mediqueue.test", "/account/login")]
    [InlineData("dashboard403@mediqueue.test", "/account/access-denied")]
    public async Task Dashboard_authentication_error_uses_safe_redirect(string email, string expectedLocation)
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage login = await LoginAsync(client, email);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        using HttpResponseMessage dashboard = await client.GetAsync("/dashboard");
        Assert.Equal(HttpStatusCode.Redirect, dashboard.StatusCode);
        Assert.Equal(expectedLocation, dashboard.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Dashboard_unavailable_renders_friendly_error_without_leaking_token()
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage login = await LoginAsync(client, "dashboardunavailable@mediqueue.test");
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        using HttpResponseMessage dashboard = await client.GetAsync("/dashboard");
        string html = WebUtility.HtmlDecode(await dashboard.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        Assert.Contains("Chưa thể kết nối Dashboard", html, StringComparison.Ordinal);
        Assert.DoesNotContain(FakeAccountApiClient.SecretToken, html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/account/login")]
    [InlineData("/account/register")]
    public async Task Authenticated_user_cannot_return_to_public_auth_pages(string path)
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage login = await LoginAsync(client, "patient@mediqueue.test");
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        using HttpResponseMessage response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/dashboard", response.Headers.Location!.OriginalString);
    }

    private async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email)
    {
        using HttpResponseMessage page = await client.GetAsync("/account/login");
        string html = await page.Content.ReadAsStringAsync();
        Match token = AntiforgeryRegex().Match(html);
        Assert.True(token.Success);
        return await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "a-valid-password",
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value)
        }));
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryRegex();

    [GeneratedRegex("<a class=\"sidebar-link active\"[^>]*aria-current=\"page\"[^>]*>.*?<span class=\"sidebar-label\">(.*?)</span></a>", RegexOptions.Singleline)]
    private static partial Regex ActiveSidebarLinkRegex();
}
