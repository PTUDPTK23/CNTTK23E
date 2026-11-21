using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediQueue.Domain.Security;
using MediQueue.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace MediQueue.Appointment.Api.Security;

public sealed class AuthenticationStateValidator(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService) : IAuthenticationStateValidator
{
    public async Task<bool> IsCurrentAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        string? subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        string? role = principal.FindFirstValue(JwtOptions.RoleClaim);
        string? authenticationVersion =
            principal.FindFirstValue(JwtOptions.AuthenticationVersionClaim);

        if (!Guid.TryParse(subject, out Guid userId) ||
            string.IsNullOrWhiteSpace(role) ||
            string.IsNullOrWhiteSpace(authenticationVersion) ||
            !SystemRoles.All.Contains(role, StringComparer.Ordinal))
        {
            return false;
        }

        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive || await userManager.IsLockedOutAsync(user))
        {
            return false;
        }

        IList<string> currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count != 1 ||
            !string.Equals(currentRoles[0], role, StringComparison.Ordinal))
        {
            return false;
        }

        string securityStamp = await userManager.GetSecurityStampAsync(user);
        return jwtTokenService.MatchesAuthenticationVersion(
            securityStamp,
            authenticationVersion);
    }
}
