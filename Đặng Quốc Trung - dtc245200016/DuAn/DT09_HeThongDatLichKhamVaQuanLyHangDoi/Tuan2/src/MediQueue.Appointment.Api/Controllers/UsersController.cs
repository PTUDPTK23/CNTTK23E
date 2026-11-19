using MediQueue.Appointment.Api.Services;
using MediQueue.Appointment.Api.Services.Users;
using MediQueue.Contracts.Users;
using MediQueue.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediQueue.Appointment.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = SystemRoles.Admin)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class UsersController(IUserManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] UserListQuery query, CancellationToken token) =>
        ToActionResult(await service.GetUsersAsync(query, token));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken token) =>
        ToActionResult(await service.GetUserAsync(id, token));

    [HttpPost]
    public async Task<IActionResult> Create(CreateStaffUserRequest request, CancellationToken token)
    {
        AccountServiceResult<UserDetailsResponse> result = await service.CreateStaffAsync(User, request, token);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetUser), new { id = result.Value!.Id }, result.Value)
            : Problem(result.Error!);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateStaffUserRequest request, CancellationToken token) =>
        ToActionResult(await service.UpdateStaffAsync(User, id, request, token));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken token) =>
        ToActionResult(await service.SetActiveAsync(User, id, false, token));

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken token) =>
        ToActionResult(await service.SetActiveAsync(User, id, true, token));

    [HttpPost("{id:guid}/unlock")]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken token) =>
        ToActionResult(await service.UnlockAsync(User, id, token));

    private IActionResult ToActionResult<T>(AccountServiceResult<T> result) =>
        result.Succeeded ? Ok(result.Value) : Problem(result.Error!);
    private IActionResult ToActionResult(AccountServiceResult result) =>
        result.Succeeded ? NoContent() : Problem(result.Error!);
    private IActionResult Problem(AccountServiceError error)
    {
        if (error.ValidationErrors is not null)
        {
            var details = new ValidationProblemDetails(error.ValidationErrors.ToDictionary())
            {
                Status = error.StatusCode,
                Title = error.Title,
                Detail = error.Detail
            };
            return StatusCode(error.StatusCode, details);
        }
        return Problem(statusCode: error.StatusCode, title: error.Title, detail: error.Detail);
    }
}
