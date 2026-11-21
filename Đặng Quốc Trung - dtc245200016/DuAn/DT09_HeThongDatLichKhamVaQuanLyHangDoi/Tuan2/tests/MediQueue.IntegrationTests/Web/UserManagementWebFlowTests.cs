using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MediQueue.IntegrationTests.Web;

public sealed partial class UserManagementWebFlowTests : IClassFixture<MediQueueWebFactory>
{
    private readonly MediQueueWebFactory factory;

    public UserManagementWebFlowTests(MediQueueWebFactory factory)
    {
        this.factory = factory;
        factory.UsersApi.Reset();
    }

    [Fact]
    public async Task Anonymous_redirects_and_non_admin_is_forbidden()
    {
        using HttpClient anonymous = Client();
        using var first = await anonymous.GetAsync("/Users");
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);

        using HttpClient patient = Client();
        await LoginAsync(patient, "patient@mediqueue.test");
        using var denied = await patient.GetAsync("/Users");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.Equal("/account/access-denied", denied.Headers.Location!.AbsolutePath);
    }

    [Fact]
    public async Task Admin_sees_user_management_and_assets_without_token_leak()
    {
        using HttpClient client = Client();
        await LoginAsync(client, "admin@mediqueue.test");
        using var response = await client.GetAsync("/Users");
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Quản lý người dùng", html);
        Assert.Contains("Bác sĩ An", html);
        Assert.Contains("Tạo tài khoản nhân sự", html);
        Assert.DoesNotContain(FakeAccountApiClient.SecretToken, html);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/css/user-management.css")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/js/user-management.js")).StatusCode);
    }

    [Theory]
    [InlineData(FakeUsersApiScenario.ServerError, HttpStatusCode.InternalServerError, "Dịch vụ đang gặp sự cố")]
    [InlineData(FakeUsersApiScenario.Unavailable, HttpStatusCode.ServiceUnavailable, "Dịch vụ đang tạm gián đoạn")]
    [InlineData(FakeUsersApiScenario.InvalidResponse, HttpStatusCode.BadGateway, "Phản hồi từ dịch vụ không hợp lệ")]
    [InlineData(FakeUsersApiScenario.BadRequest, HttpStatusCode.BadRequest, "Yêu cầu chưa hợp lệ")]
    [InlineData(FakeUsersApiScenario.NotFound, HttpStatusCode.NotFound, "Không tìm thấy tài khoản")]
    [InlineData(FakeUsersApiScenario.Conflict, HttpStatusCode.Conflict, "Dữ liệu vừa được thay đổi")]
    public async Task Admin_receives_rendered_error_state_without_view_not_found(
        FakeUsersApiScenario scenario,
        HttpStatusCode expectedStatus,
        string expectedTitle)
    {
        factory.UsersApi.Scenario = scenario;
        using HttpClient client = Client();
        await LoginAsync(client, "admin@mediqueue.test");

        using HttpResponseMessage response = await client.GetAsync("/Users");
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Contains(expectedTitle, html, StringComparison.Ordinal);
        if (scenario == FakeUsersApiScenario.BadRequest)
            Assert.DoesNotContain("Thử tải lại", html, StringComparison.Ordinal);
        else
            Assert.Contains("Thử tải lại", html, StringComparison.Ordinal);
        Assert.Contains("Về Dashboard", html, StringComparison.Ordinal);
        Assert.DoesNotContain("The view 'Error' was not found", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Chi tiết nội bộ", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(FakeAccountApiClient.SecretToken, html, StringComparison.Ordinal);
    }

    private async Task LoginAsync(HttpClient client, string email)
    {
        using var page = await client.GetAsync("/account/login");
        string html = await page.Content.ReadAsStringAsync();
        Match token = Token().Match(html);
        using var response = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "a-valid-password",
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value)
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private HttpClient Client() => factory.CreateClient(new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex Token();
}
