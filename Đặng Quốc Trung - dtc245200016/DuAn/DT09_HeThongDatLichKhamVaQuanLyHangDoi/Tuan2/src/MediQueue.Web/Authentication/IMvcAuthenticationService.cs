using System.Security.Claims;
using MediQueue.Contracts.Authentication;

namespace MediQueue.Web.Authentication;

public interface IMvcAuthenticationService
{
    Task SignInAsync(HttpContext httpContext, LoginResponse login, CancellationToken cancellationToken);
    Task<AuthenticationSession?> GetCurrentSessionAsync(HttpContext httpContext, CancellationToken cancellationToken);
    Task RefreshPrincipalAsync(HttpContext httpContext, AccountProfileResponse profile, CancellationToken cancellationToken);
    Task ClearLocalSessionAsync(HttpContext httpContext, CancellationToken cancellationToken);
}

public static class MediQueueClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    public static string? GetSessionId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(MediQueueAuthenticationDefaults.SessionIdClaim);
}
