using System.Net;
using System.Security.Claims;
using MediQueue.Contracts.Dashboard;
using MediQueue.Web.Authentication;
using MediQueue.Web.Clients;
using MediQueue.Web.Models.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediQueue.Web.Controllers;

[Authorize(Roles = "Admin,Doctor,Receptionist,Patient")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class DashboardController(
    IDashboardApiClient dashboardApiClient,
    IMvcAuthenticationService authenticationService) : Controller
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        AuthenticationSession? session = await authenticationService.GetCurrentSessionAsync(
            HttpContext,
            cancellationToken);
        if (session is null)
        {
            return await EndSessionAsync(cancellationToken);
        }

        ApiClientResult<DashboardResponse> result = await dashboardApiClient.GetDashboardAsync(
            session.AccessToken,
            cancellationToken);
        if (!result.Succeeded)
        {
            return await HandleFailureAsync(result.Problem!, cancellationToken);
        }

        string? currentRole = User.FindFirstValue(ClaimTypes.Role);
        DashboardResponse dashboard = result.Value!;
        if (!string.Equals(currentRole, dashboard.Role, StringComparison.Ordinal))
        {
            await authenticationService.ClearLocalSessionAsync(HttpContext, cancellationToken);
            return RedirectToAction("AccessDenied", "Account");
        }

        return dashboard.Role switch
        {
            "Admin" when dashboard.Admin is not null =>
                View("Admin", new AdminDashboardViewModel(dashboard)),
            "Doctor" when dashboard.Doctor is not null =>
                View("Doctor", new DoctorDashboardViewModel(dashboard)),
            "Receptionist" when dashboard.Receptionist is not null =>
                View("Receptionist", new ReceptionistDashboardViewModel(dashboard)),
            "Patient" when dashboard.Patient is not null =>
                View("Patient", new PatientDashboardViewModel(dashboard)),
            _ => View("Error", new DashboardStateViewModel(
                "Dashboard chưa sẵn sàng",
                "Dữ liệu trả về chưa phù hợp với vai trò của tài khoản.",
                "alert",
                "error",
                true))
        };
    }

    private async Task<IActionResult> HandleFailureAsync(
        ApiProblem problem,
        CancellationToken cancellationToken)
    {
        if (problem.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await EndSessionAsync(cancellationToken);
        }

        if (problem.StatusCode == HttpStatusCode.Forbidden)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        return View("Error", new DashboardStateViewModel(
            problem.IsUnavailable ? "Chưa thể kết nối Dashboard" : problem.Title,
            problem.Detail,
            "alert",
            "error",
            true));
    }

    private async Task<IActionResult> EndSessionAsync(CancellationToken cancellationToken)
    {
        await authenticationService.ClearLocalSessionAsync(HttpContext, cancellationToken);
        TempData["WarningMessage"] = "Phiên đăng nhập đã hết hạn hoặc bị thu hồi. Vui lòng đăng nhập lại.";
        return RedirectToAction("Login", "Account");
    }
}
