using System.Security.Claims;
using System.Security.Cryptography;
using MediQueue.Contracts.Authentication;
using Microsoft.AspNetCore.Authentication;

namespace MediQueue.Web.Authentication;

public sealed class MvcAuthenticationService(
    IAuthenticationSessionStore sessionStore,
    TimeProvider timeProvider) : IMvcAuthenticationService
{
    private static readonly TimeSpan CookieLifetime = TimeSpan.FromMinutes(30);

    public async Task SignInAsync(
        HttpContext httpContext,
        LoginResponse login,
        CancellationToken cancellationToken)
    {
        DateTimeOffset expiresAtUtc = Min(
            login.ExpiresAtUtc,
            timeProvider.GetUtcNow().Add(CookieLifetime));
        string sessionId = CreateSessionId();
        var session = new AuthenticationSession(
            sessionId,
            login.UserId,
            login.AccessToken,
            expiresAtUtc);
        await sessionStore.StoreAsync(session, cancellationToken);

        ClaimsPrincipal principal = CreatePrincipal(
            login.UserId,
            login.Email,
            login.FullName,
            login.Role,
            sessionId);
        await httpContext.SignInAsync(
            MediQueueAuthenticationDefaults.Scheme,
            principal,
            new AuthenticationProperties
            {
                AllowRefresh = false,
                ExpiresUtc = expiresAtUtc,
                IsPersistent = false,
                IssuedUtc = timeProvider.GetUtcNow()
            });
    }

    public Task<AuthenticationSession?> GetCurrentSessionAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        string? sessionId = httpContext.User.GetSessionId();
        return sessionId is not null && httpContext.User.TryGetUserId(out Guid userId)
            ? sessionStore.GetAsync(sessionId, userId, cancellationToken)
            : Task.FromResult<AuthenticationSession?>(null);
    }

    public async Task RefreshPrincipalAsync(
        HttpContext httpContext,
        AccountProfileResponse profile,
        CancellationToken cancellationToken)
    {
        AuthenticationSession? session = await GetCurrentSessionAsync(httpContext, cancellationToken);
        if (session is null)
        {
            return;
        }

        ClaimsPrincipal principal = CreatePrincipal(
            profile.UserId,
            profile.Email,
            profile.FullName,
            profile.Role,
            session.SessionId);
        await httpContext.SignInAsync(
            MediQueueAuthenticationDefaults.Scheme,
            principal,
            new AuthenticationProperties
            {
                AllowRefresh = false,
                ExpiresUtc = session.ExpiresAtUtc,
                IsPersistent = false
            });
    }

    public async Task ClearLocalSessionAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        string? sessionId = httpContext.User.GetSessionId();
        if (sessionId is not null)
        {
            await sessionStore.RemoveAsync(sessionId, cancellationToken);
        }

        await httpContext.SignOutAsync(MediQueueAuthenticationDefaults.Scheme);
    }

    private static ClaimsPrincipal CreatePrincipal(
        Guid userId,
        string email,
        string? fullName,
        string role,
        string sessionId)
    {
        string displayName = string.IsNullOrWhiteSpace(fullName) ? email : fullName;
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Role, role),
            new(MediQueueAuthenticationDefaults.DisplayNameClaim, displayName),
            new(MediQueueAuthenticationDefaults.SessionIdClaim, sessionId)
        ];
        return new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            MediQueueAuthenticationDefaults.Scheme,
            ClaimTypes.Name,
            ClaimTypes.Role));
    }

    private static string CreateSessionId() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right) =>
        left <= right ? left : right;
}
