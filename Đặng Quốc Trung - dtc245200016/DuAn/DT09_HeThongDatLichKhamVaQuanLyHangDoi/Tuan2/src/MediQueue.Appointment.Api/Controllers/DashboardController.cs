using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediQueue.Appointment.Api.Security;
using MediQueue.Appointment.Api.Services.Dashboard;
using MediQueue.Contracts.Dashboard;
using MediQueue.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediQueue.Appointment.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        string? subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        string? role = User.FindFirstValue(JwtOptions.RoleClaim);
        if (!Guid.TryParse(subject, out Guid userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(role) ||
            !SystemRoles.All.Contains(role, StringComparer.Ordinal))
        {
            return Forbid();
        }

        DashboardResponse? response = await dashboardService.GetForUserAsync(
            userId,
            role,
            cancellationToken);
        if (response is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Không tìm thấy dữ liệu Dashboard",
                detail: "Tài khoản chưa có dữ liệu phù hợp để hiển thị Dashboard.");
        }

        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return Ok(response);
    }
}
