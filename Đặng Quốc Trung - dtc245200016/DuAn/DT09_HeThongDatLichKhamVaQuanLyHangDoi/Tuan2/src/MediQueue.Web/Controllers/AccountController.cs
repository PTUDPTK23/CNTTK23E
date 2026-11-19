using System.Net;
using MediQueue.Contracts.Authentication;
using MediQueue.Web.Authentication;
using MediQueue.Web.Clients;
using MediQueue.Web.Models.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediQueue.Web.Controllers;

[AutoValidateAntiforgeryToken]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class AccountController(
    IAccountApiClient accountApiClient,
    IMvcAuthenticationService authenticationService) : Controller
{
    [AllowAnonymous]
    [HttpGet("account/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new LoginViewModel
        {
            ReturnUrl = IsSafeReturnUrl(returnUrl) ? returnUrl : null
        });
    }

    [AllowAnonymous]
    [HttpPost("account/login")]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        CancellationToken cancellationToken)
    {
        model.ReturnUrl = IsSafeReturnUrl(model.ReturnUrl) ? model.ReturnUrl : null;
        if (!ModelState.IsValid)
        {
            ClearPassword(model);
            return View(model);
        }

        ApiClientResult<LoginResponse> result = await accountApiClient.LoginAsync(
            new LoginRequest { Email = model.Email, Password = model.Password },
            cancellationToken);
        if (!result.Succeeded)
        {
            ApplyProblem(result.Problem!);
            ClearPassword(model);
            return View(model);
        }

        await authenticationService.SignInAsync(HttpContext, result.Value!, cancellationToken);
        return model.ReturnUrl is not null
            ? LocalRedirect(model.ReturnUrl)
            : RedirectToAction("Index", "Dashboard");
    }

    [AllowAnonymous]
    [HttpGet("account/register")]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View(new RegisterViewModel());
    }

    [AllowAnonymous]
    [HttpPost("account/register")]
    public async Task<IActionResult> Register(
        RegisterViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ClearPasswords(model);
            return View(model);
        }

        ApiClientResult<AccountProfileResponse> result = await accountApiClient.RegisterAsync(
            new RegisterPatientRequest
            {
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword
            },
            cancellationToken);
        if (!result.Succeeded)
        {
            ApplyProblem(result.Problem!);
            ClearPasswords(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Tài khoản bệnh nhân đã được tạo. Bạn có thể đăng nhập ngay.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet("account/profile")]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        AuthenticationSession? session = await authenticationService.GetCurrentSessionAsync(
            HttpContext,
            cancellationToken);
        if (session is null)
        {
            return await EndSessionAndRedirectAsync(cancellationToken);
        }

        ApiClientResult<AccountProfileResponse> result = await accountApiClient.GetProfileAsync(
            session.AccessToken,
            cancellationToken);
        IActionResult? failure = await HandleAuthenticatedFailureAsync(result.Problem, cancellationToken);
        return failure ?? View(ToProfileViewModel(result.Value!));
    }

    [Authorize]
    [HttpPost("account/profile")]
    public async Task<IActionResult> Profile(
        ProfileViewModel model,
        CancellationToken cancellationToken)
    {
        string role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        model.Role = role;
        model.Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? model.Email;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        AuthenticationSession? session = await authenticationService.GetCurrentSessionAsync(
            HttpContext,
            cancellationToken);
        if (session is null)
        {
            return await EndSessionAndRedirectAsync(cancellationToken);
        }

        ApiClientResult<AccountProfileResponse> result = await accountApiClient.UpdateProfileAsync(
            session.AccessToken,
            new UpdateProfileRequest
            {
                FullName = role is "Patient" or "Doctor" or "Receptionist" or "Admin" ? model.FullName : null,
                PhoneNumber = model.PhoneNumber
            },
            cancellationToken);
        if (!result.Succeeded)
        {
            IActionResult? failure = await HandleAuthenticatedFailureAsync(
                result.Problem,
                cancellationToken,
                applyValidation: true);
            return failure ?? View(model);
        }

        await authenticationService.RefreshPrincipalAsync(HttpContext, result.Value!, cancellationToken);
        TempData["SuccessMessage"] = "Thông tin cá nhân đã được cập nhật.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet("account/change-password")]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize]
    [HttpPost("account/change-password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ClearPasswords(model);
            return View(model);
        }

        AuthenticationSession? session = await authenticationService.GetCurrentSessionAsync(
            HttpContext,
            cancellationToken);
        if (session is null)
        {
            return await EndSessionAndRedirectAsync(cancellationToken);
        }

        ApiClientResult result = await accountApiClient.ChangePasswordAsync(
            session.AccessToken,
            new ChangePasswordRequest
            {
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword,
                ConfirmNewPassword = model.ConfirmNewPassword
            },
            cancellationToken);
        if (!result.Succeeded)
        {
            IActionResult? failure = await HandleAuthenticatedFailureAsync(
                result.Problem,
                cancellationToken,
                applyValidation: true);
            ClearPasswords(model);
            return failure ?? View(model);
        }

        await authenticationService.ClearLocalSessionAsync(HttpContext, cancellationToken);
        TempData["SuccessMessage"] = "Mật khẩu đã được đổi. Vui lòng đăng nhập lại để tiếp tục.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpPost("account/logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        AuthenticationSession? session = await authenticationService.GetCurrentSessionAsync(
            HttpContext,
            cancellationToken);
        ApiClientResult? apiResult = session is null
            ? null
            : await accountApiClient.LogoutAsync(session.AccessToken, cancellationToken);

        await authenticationService.ClearLocalSessionAsync(HttpContext, cancellationToken);
        TempData[apiResult?.Problem?.IsUnavailable == true ? "WarningMessage" : "SuccessMessage"] =
            apiResult?.Problem?.IsUnavailable == true
                ? "Bạn đã đăng xuất trên thiết bị này. Dịch vụ xác thực đang tạm gián đoạn."
                : "Bạn đã đăng xuất an toàn.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet("account/access-denied")]
    public IActionResult AccessDenied() => View();

    private bool IsSafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl);

    private async Task<IActionResult?> HandleAuthenticatedFailureAsync(
        ApiProblem? problem,
        CancellationToken cancellationToken,
        bool applyValidation = false)
    {
        if (problem is null)
        {
            return null;
        }

        if (problem.StatusCode == HttpStatusCode.Unauthorized)
        {
            return await EndSessionAndRedirectAsync(cancellationToken);
        }

        if (problem.StatusCode == HttpStatusCode.Forbidden)
        {
            return RedirectToAction(nameof(AccessDenied));
        }

        if (problem.IsUnavailable)
        {
            TempData["ErrorMessage"] = problem.Detail;
            return RedirectToAction("Error", "Home");
        }

        if (applyValidation)
        {
            ApplyProblem(problem);
            return null;
        }

        TempData["ErrorMessage"] = problem.Detail;
        return RedirectToAction("Error", "Home");
    }

    private async Task<IActionResult> EndSessionAndRedirectAsync(CancellationToken cancellationToken)
    {
        await authenticationService.ClearLocalSessionAsync(HttpContext, cancellationToken);
        TempData["WarningMessage"] = "Phiên đăng nhập đã hết hạn hoặc bị thu hồi. Vui lòng đăng nhập lại.";
        return RedirectToAction(nameof(Login));
    }

    private void ApplyProblem(ApiProblem problem)
    {
        foreach ((string key, string[] messages) in problem.ValidationErrors)
        {
            string viewModelKey = key switch
            {
                "fullName" => nameof(RegisterViewModel.FullName),
                "email" => nameof(RegisterViewModel.Email),
                "phoneNumber" => nameof(RegisterViewModel.PhoneNumber),
                "dateOfBirth" => nameof(RegisterViewModel.DateOfBirth),
                "password" => nameof(RegisterViewModel.Password),
                "confirmPassword" => nameof(RegisterViewModel.ConfirmPassword),
                "currentPassword" => nameof(ChangePasswordViewModel.CurrentPassword),
                "newPassword" => nameof(ChangePasswordViewModel.NewPassword),
                "confirmNewPassword" => nameof(ChangePasswordViewModel.ConfirmNewPassword),
                _ => string.Empty
            };
            foreach (string message in messages)
            {
                ModelState.AddModelError(viewModelKey, message);
            }
        }

        if (problem.ValidationErrors.Count == 0)
        {
            ModelState.AddModelError(string.Empty, problem.Detail);
        }
    }

    private void ClearPassword(LoginViewModel model)
    {
        model.Password = string.Empty;
        ModelState.Remove(nameof(LoginViewModel.Password));
    }

    private void ClearPasswords(RegisterViewModel model)
    {
        model.Password = string.Empty;
        model.ConfirmPassword = string.Empty;
        ModelState.Remove(nameof(RegisterViewModel.Password));
        ModelState.Remove(nameof(RegisterViewModel.ConfirmPassword));
    }

    private void ClearPasswords(ChangePasswordViewModel model)
    {
        model.CurrentPassword = string.Empty;
        model.NewPassword = string.Empty;
        model.ConfirmNewPassword = string.Empty;
        ModelState.Remove(nameof(ChangePasswordViewModel.CurrentPassword));
        ModelState.Remove(nameof(ChangePasswordViewModel.NewPassword));
        ModelState.Remove(nameof(ChangePasswordViewModel.ConfirmNewPassword));
    }

    private static ProfileViewModel ToProfileViewModel(AccountProfileResponse profile) => new()
    {
        UserId = profile.UserId,
        FullName = profile.FullName,
        Email = profile.Email,
        PhoneNumber = profile.PhoneNumber,
        Role = profile.Role,
        DateOfBirth = profile.DateOfBirth
    };
}
