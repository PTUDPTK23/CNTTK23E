using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

namespace Campaign.Api;

internal sealed class AdminAccess(IConfiguration config)
{
    internal const string Role = "Admin";
    internal const string DemoPassword = "notifylab_demo_admin";
    private const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;
    private readonly string username = config["Admin:Username"] ?? "admin";
    private readonly byte[] passwordDigest = LoadPassword(config);

    internal static void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new AdminAccess(configuration));
        services.AddAuthentication(Scheme).AddCookie(options =>
        {
            options.Cookie.Name = "NotifyLab.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromHours(4);
            options.SlidingExpiration = false;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });
        services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().RequireRole(Role).Build());
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("admin-login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
        });
    }

    internal static void MapEndpoints(WebApplication app)
    {
        app.MapPost("/auth/login", async (LoginRequest input, HttpContext context, AdminAccess admin) =>
        {
            if (!admin.Matches(input))
                return Results.Json(new { error = "Tên đăng nhập hoặc mật khẩu không đúng." }, statusCode: 401);
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, admin.username), new Claim(ClaimTypes.Role, Role)], Scheme);
            await context.SignInAsync(Scheme, new ClaimsPrincipal(identity));
            return Results.NoContent();
        }).AllowAnonymous().RequireRateLimiting("admin-login");

        app.MapGet("/auth/session", (ClaimsPrincipal user) => Results.Ok(new { username = user.Identity?.Name }));
        app.MapPost("/auth/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(Scheme);
            return Results.NoContent();
        });
    }

    private bool Matches(LoginRequest input)
    {
        if (input.Username is null || input.Password is null || input.Username.Length > 120 || input.Password.Length > 256)
            return false;
        var candidate = SHA256.HashData(Encoding.UTF8.GetBytes(input.Password));
        var passwordMatches = CryptographicOperations.FixedTimeEquals(candidate, passwordDigest);
        return passwordMatches && string.Equals(username, input.Username, StringComparison.Ordinal);
    }

    private static byte[] LoadPassword(IConfiguration config)
    {
        var password = config["Admin:Password"] ?? DemoPassword;
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 256 ||
            (config.GetValue("Delivery:ExternalSmtp", false) && password == DemoPassword))
            throw new InvalidOperationException("SMTP thật cần ADMIN_PASSWORD riêng, dài từ 12 đến 256 ký tự.");
        return SHA256.HashData(Encoding.UTF8.GetBytes(password));
    }

    public sealed record LoginRequest(string? Username, string? Password);
}
