using System.Net;
using MediQueue.Contracts.Users;
using MediQueue.Web.Authentication;
using MediQueue.Web.Clients;
using MediQueue.Web.Models.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediQueue.Web.Controllers;

[Authorize(Roles = "Admin")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[Route("Users")]
public sealed class UsersController(IUsersApiClient usersApiClient, IMvcAuthenticationService authenticationService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] UserListQuery query, CancellationToken ct)
    {
        AuthenticationSession? session = await SessionAsync(ct);
        if (session is null) return EndSession();
        ApiClientResult<PagedUsersResponse> result = await usersApiClient.GetUsersAsync(session.AccessToken, query, ct);
        if (!result.Succeeded) return Failure(result.Problem!);
        return View(new UsersIndexViewModel(query, result.Value!));
    }

    [HttpGet("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var result = await GetAsync(id, ct);
        return result.Result ?? View(result.Value);
    }

    [HttpGet("Create")]
    public IActionResult Create() => View(new CreateStaffViewModel());

    [HttpPost("Create"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStaffViewModel model, CancellationToken ct)
    {
        if (!UserManagementValues.StaffRoles.Contains(model.Role, StringComparer.Ordinal))
            ModelState.AddModelError(nameof(model.Role), "Chỉ được chọn Bác sĩ hoặc Lễ tân.");
        if (!ModelState.IsValid) return View(model);
        AuthenticationSession? session = await SessionAsync(ct);
        if (session is null) return EndSession();
        ApiClientResult<UserDetailsResponse> result = await usersApiClient.CreateAsync(session.AccessToken,
            new() { FullName = model.FullName, Email = model.Email, PhoneNumber = model.PhoneNumber, Role = model.Role, Password = model.Password }, ct);
        model.Password = model.ConfirmPassword = string.Empty;
        if (!result.Succeeded)
        {
            ApiProblem problem = result.Problem!;
            if (problem.StatusCode == HttpStatusCode.Unauthorized) return EndSession();
            if (problem.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Account");
            if (problem.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
            {
                ApplyProblem(problem);
                return View(model);
            }
            return Failure(problem);
        }
        TempData["SuccessMessage"] = "Đã tạo tài khoản nhân sự.";
        return RedirectToAction(nameof(Details), new { id = result.Value!.Id });
    }

    [HttpGet("Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var result = await GetAsync(id, ct);
        if (result.Result is not null) return result.Result;
        UserDetailsResponse user = result.Value!;
        if (!UserManagementValues.StaffRoles.Contains(user.Role, StringComparer.Ordinal)) return RedirectToAction(nameof(Details), new { id });
        return View(new EditStaffViewModel { Id = id, Email = user.Email, Role = user.Role, FullName = user.DisplayName, PhoneNumber = user.PhoneNumber });
    }

    [HttpPost("Edit/{id:guid}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditStaffViewModel model, CancellationToken ct)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);
        AuthenticationSession? session = await SessionAsync(ct);
        if (session is null) return EndSession();
        var result = await usersApiClient.UpdateAsync(session.AccessToken, id, new() { FullName = model.FullName, PhoneNumber = model.PhoneNumber }, ct);
        if (!result.Succeeded)
        {
            ApiProblem problem = result.Problem!;
            if (problem.StatusCode == HttpStatusCode.Unauthorized) return EndSession();
            if (problem.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Account");
            if (problem.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
            {
                ApplyProblem(problem);
                return View(model);
            }
            return Failure(problem);
        }
        TempData["SuccessMessage"] = "Đã cập nhật thông tin nhân sự.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/deactivate"), ValidateAntiForgeryToken]
    public Task<IActionResult> Deactivate(Guid id, CancellationToken ct) => CommandAsync(id, "vô hiệu hóa", usersApiClient.DeactivateAsync, ct);
    [HttpPost("{id:guid}/activate"), ValidateAntiForgeryToken]
    public Task<IActionResult> Activate(Guid id, CancellationToken ct) => CommandAsync(id, "kích hoạt", usersApiClient.ActivateAsync, ct);
    [HttpPost("{id:guid}/unlock"), ValidateAntiForgeryToken]
    public Task<IActionResult> Unlock(Guid id, CancellationToken ct) => CommandAsync(id, "mở khóa đăng nhập", usersApiClient.UnlockAsync, ct);

    private async Task<IActionResult> CommandAsync(Guid id, string verb, Func<string, Guid, CancellationToken, Task<ApiClientResult>> command, CancellationToken ct)
    {
        AuthenticationSession? session = await SessionAsync(ct);
        if (session is null) return EndSession();
        ApiClientResult result = await command(session.AccessToken, id, ct);
        if (!result.Succeeded) return Failure(result.Problem!);
        TempData["SuccessMessage"] = $"Đã {verb} tài khoản.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<(UserDetailsResponse? Value, IActionResult? Result)> GetAsync(Guid id, CancellationToken ct)
    {
        AuthenticationSession? session = await SessionAsync(ct);
        if (session is null) return (null, EndSession());
        var result = await usersApiClient.GetUserAsync(session.AccessToken, id, ct);
        return result.Succeeded ? (result.Value, null) : (null, Failure(result.Problem!));
    }
    private Task<AuthenticationSession?> SessionAsync(CancellationToken ct) => authenticationService.GetCurrentSessionAsync(HttpContext, ct);
    private IActionResult EndSession() { TempData["WarningMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."; return RedirectToAction("Login", "Account"); }
    private IActionResult Failure(ApiProblem problem)
    {
        if (problem.StatusCode == HttpStatusCode.Unauthorized) return EndSession();
        if (problem.StatusCode == HttpStatusCode.Forbidden) return RedirectToAction("AccessDenied", "Account");

        int statusCode = (int)(problem.StatusCode ?? HttpStatusCode.ServiceUnavailable);
        if (statusCode < 400) statusCode = StatusCodes.Status500InternalServerError;
        Response.StatusCode = statusCode;
        var model = new UserManagementErrorViewModel(
            GetErrorTitle(statusCode),
            GetErrorDescription(problem, statusCode),
            statusCode,
            statusCode is not StatusCodes.Status400BadRequest);
        return View("~/Views/Shared/UserManagementError.cshtml", model);
    }

    private static string GetErrorTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Yêu cầu chưa hợp lệ",
        StatusCodes.Status404NotFound => "Không tìm thấy tài khoản",
        StatusCodes.Status409Conflict => "Dữ liệu vừa được thay đổi",
        StatusCodes.Status502BadGateway => "Phản hồi từ dịch vụ không hợp lệ",
        StatusCodes.Status503ServiceUnavailable => "Dịch vụ đang tạm gián đoạn",
        _ when statusCode >= 500 => "Dịch vụ đang gặp sự cố",
        _ => "Chưa thể xử lý yêu cầu"
    };

    private static string GetErrorDescription(ApiProblem problem, int statusCode) => statusCode >= 500
        ? "Chúng tôi chưa thể tải dữ liệu người dùng vào lúc này. Vui lòng thử lại sau."
        : problem.Detail;
    private void ApplyProblem(ApiProblem problem)
    {
        foreach ((string key, string[] messages) in problem.ValidationErrors)
            foreach (string message in messages) ModelState.AddModelError(key, message);
        if (problem.ValidationErrors.Count == 0) ModelState.AddModelError(string.Empty, problem.Detail);
    }
}
