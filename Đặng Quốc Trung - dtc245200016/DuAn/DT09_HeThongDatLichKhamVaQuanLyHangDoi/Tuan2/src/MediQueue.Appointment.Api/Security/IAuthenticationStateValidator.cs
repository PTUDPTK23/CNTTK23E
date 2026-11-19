using System.Security.Claims;

namespace MediQueue.Appointment.Api.Security;

public interface IAuthenticationStateValidator
{
    Task<bool> IsCurrentAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
