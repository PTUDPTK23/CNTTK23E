using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MediQueue.IntegrationTests.Web;

public sealed partial class AccountWebFlowTests : IClassFixture<MediQueueWebFactory>
{
    private readonly MediQueueWebFactory factory;

    public AccountWebFlowTests(MediQueueWebFactory factory)
    {
        this.factory = factory;
        factory.AccountApi.Reset();
    }

    [Fact]
    public async Task Anonymous_profile_redirects_to_shared_login_with_local_return_url()
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/account/profile");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login", response.Headers.Location!.AbsolutePath);
        Assert.Contains("returnUrl=%2Faccount%2Fprofile", response.Headers.Location.Query, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_success_sets_secure_cookie_without_exposing_jwt()
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await PostWithAntiforgeryAsync(client, "/account/login", new()
        {
            ["Email"] = "patient@mediqueue.test",
            ["Password"] = "a-valid-password"
        });
        string headers = response.Headers.ToString();

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/profile", response.Headers.Location!.OriginalString);
        Assert.Contains(".MediQueue.Auth=", headers, StringComparison.Ordinal);
        Assert.Contains("httponly", headers, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", headers, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", headers, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(FakeAccountApiClient.SecretToken, headers, StringComparison.Ordinal);
        Assert.DoesNotContain(FakeAccountApiClient.SecretToken, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_failure_is_generic_and_never_repopulates_password()
    {
        factory.AccountApi.RejectLogin = true;
        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await PostWithAntiforgeryAsync(client, "/account/login", new()
        {
            ["Email"] = "unknown@example.com",
            ["Password"] = "secret-value-that-must-disappear"
        });
        string html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            "Email hoặc mật khẩu không chính xác",
            WebUtility.HtmlDecode(html),
            StringComparison.Ordinal);
        Assert.DoesNotContain("secret-value-that-must-disappear", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unsafe_external_return_url_is_ignored()
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await PostWithAntiforgeryAsync(client, "/account/login", new()
        {
            ["Email"] = "patient@mediqueue.test",
            ["Password"] = "a-valid-password",
            ["ReturnUrl"] = "https://evil.example/steal"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/profile", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task State_changing_form_rejects_missing_antiforgery_token()
    {
        using HttpClient client = CreateClient();
        using HttpResponseMessage response = await client.PostAsync(
            "/account/login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = "patient@mediqueue.test",
                ["Password"] = "a-valid-password"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_profile_can_be_read_and_updated_through_api_client()
    {
        using HttpClient client = CreateClient();
        await LoginAsync(client);

        using HttpResponseMessage page = await client.GetAsync("/account/profile");
        string html = await page.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Thông tin cá nhân", html, StringComparison.Ordinal);
        Assert.DoesNotContain(FakeAccountApiClient.SecretToken, html, StringComparison.Ordinal);

        using HttpResponseMessage update = await PostWithPageTokenAsync(client, "/account/profile", html, new()
        {
            ["UserId"] = FakeAccountApiClient.UserId.ToString(),
            ["Role"] = "Patient",
            ["Email"] = "patient@mediqueue.test",
            ["FullName"] = "Nguyễn An Mới",
            ["PhoneNumber"] = "0912345678"
        });
        Assert.Equal(HttpStatusCode.Redirect, update.StatusCode);
        Assert.Equal("Nguyễn An Mới", factory.AccountApi.LastProfileUpdate!.FullName);
    }

    [Fact]
    public async Task Change_password_clears_session_and_requires_new_login()
    {
        using HttpClient client = CreateClient();
        await LoginAsync(client);
        using HttpResponseMessage page = await client.GetAsync("/account/change-password");
        string html = await page.Content.ReadAsStringAsync();

        using HttpResponseMessage changed = await PostWithPageTokenAsync(client, "/account/change-password", html, new()
        {
            ["CurrentPassword"] = "current-password",
            ["NewPassword"] = "new-secure-password",
            ["ConfirmNewPassword"] = "new-secure-password"
        });

        Assert.Equal(HttpStatusCode.Redirect, changed.StatusCode);
        Assert.Equal("/account/login", changed.Headers.Location!.OriginalString);
        Assert.NotNull(factory.AccountApi.LastPasswordChange);
        using HttpResponseMessage profile = await client.GetAsync("/account/profile");
        Assert.Equal(HttpStatusCode.Redirect, profile.StatusCode);
    }

    [Fact]
    public async Task Logout_is_post_only_calls_backend_and_clears_local_cookie()
    {
        using HttpClient client = CreateClient();
        await LoginAsync(client);

        using HttpResponseMessage get = await client.GetAsync("/account/logout");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);

        using HttpResponseMessage profile = await client.GetAsync("/account/profile");
        string html = await profile.Content.ReadAsStringAsync();
        using HttpResponseMessage logout = await PostWithPageTokenAsync(client, "/account/logout", html, []);
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal(1, factory.AccountApi.LogoutCalls);
        Assert.Contains(".MediQueue.Auth=;", logout.Headers.ToString(), StringComparison.Ordinal);
    }

    private async Task LoginAsync(HttpClient client)
    {
        using HttpResponseMessage response = await PostWithAntiforgeryAsync(client, "/account/login", new()
        {
            ["Email"] = "patient@mediqueue.test",
            ["Password"] = "a-valid-password"
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostWithAntiforgeryAsync(
        HttpClient client,
        string path,
        Dictionary<string, string> values)
    {
        using HttpResponseMessage page = await client.GetAsync(path);
        string html = await page.Content.ReadAsStringAsync();
        return await PostWithPageTokenAsync(client, path, html, values);
    }

    private static Task<HttpResponseMessage> PostWithPageTokenAsync(
        HttpClient client,
        string path,
        string html,
        Dictionary<string, string> values)
    {
        Match match = AntiforgeryRegex().Match(html);
        Assert.True(match.Success, "Trang phải có antiforgery token.");
        values["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value);
        return client.PostAsync(path, new FormUrlEncodedContent(values));
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryRegex();
}
