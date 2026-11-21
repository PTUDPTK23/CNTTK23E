using MediQueue.Appointment.Api.Security;
using MediQueue.Appointment.Api.Services;
using MediQueue.Contracts.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MediQueue.Appointment.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public sealed class AccountController(IAccountService accountService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(MediQueueAuthenticationExtensions.RegisterPolicy)]
    [ProducesResponseType<AccountProfileResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(
        RegisterPatientRequest request,
        CancellationToken cancellationToken)
    {
        AccountServiceResult<AccountProfileResponse> result =
            await accountService.RegisterPatientAsync(request, cancellationToken);
        return result.Succeeded
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : ToProblem(result.Error!);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(MediQueueAuthenticationExtensions.LoginPolicy)]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        AccountServiceResult<LoginResponse> result =
            await accountService.LoginAsync(request, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return result.Succeeded ? Ok(result.Value) : ToProblem(result.Error!);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        AccountServiceResult result = await accountService.LogoutAsync(User, cancellationToken);
        return result.Succeeded ? NoContent() : ToProblem(result.Error!);
    }

    [HttpGet("me")]
    [ProducesResponseType<AccountProfileResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        AccountServiceResult<AccountProfileResponse> result =
            await accountService.GetProfileAsync(User, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : ToProblem(result.Error!);
    }

    [HttpPut("profile")]
    [ProducesResponseType<AccountProfileResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        AccountServiceResult<AccountProfileResponse> result =
            await accountService.UpdateProfileAsync(User, request, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : ToProblem(result.Error!);
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        AccountServiceResult result =
            await accountService.ChangePasswordAsync(User, request, cancellationToken);
        return result.Succeeded ? NoContent() : ToProblem(result.Error!);
    }

    private IActionResult ToProblem(AccountServiceError error)
    {
        if (error.ValidationErrors is not null)
        {
            return BadRequest(new ValidationProblemDetails(
                new Dictionary<string, string[]>(error.ValidationErrors))
            {
                Status = error.StatusCode,
                Title = error.Title,
                Detail = error.Detail
            });
        }

        return Problem(
            statusCode: error.StatusCode,
            title: error.Title,
            detail: error.Detail);
    }
}
