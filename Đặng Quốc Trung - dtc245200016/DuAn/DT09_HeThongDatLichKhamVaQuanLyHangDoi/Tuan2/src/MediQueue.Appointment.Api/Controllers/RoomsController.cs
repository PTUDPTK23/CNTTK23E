using MediQueue.Appointment.Api.Services;
using MediQueue.Appointment.Api.Services.Catalogs;
using MediQueue.Contracts.Catalogs;
using MediQueue.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediQueue.Appointment.Api.Controllers;

[ApiController, Route("api/rooms"), Authorize(Roles = SystemRoles.Admin)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class RoomsController(IRoomService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> Index([FromQuery] RoomListQuery query, CancellationToken token) => Result(await service.GetAsync(query, token));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Details(Guid id, CancellationToken token) => Result(await service.GetAsync(id, token));
    [HttpPost]
    public async Task<IActionResult> Create(CreateRoomRequest request, CancellationToken token)
    {
        AccountServiceResult<RoomDetailsResponse> result = await service.CreateAsync(User, request, token);
        return result.Succeeded ? CreatedAtAction(nameof(Details), new { id = result.Value!.Id }, result.Value) : Problem(result.Error!);
    }
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, UpdateRoomRequest request, CancellationToken token) => Result(await service.UpdateAsync(User, id, request, token));
    [HttpPost("{id:guid}/activate")] public async Task<IActionResult> Activate(Guid id, CancellationToken token) => Result(await service.SetActiveAsync(User, id, true, token));
    [HttpPost("{id:guid}/deactivate")] public async Task<IActionResult> Deactivate(Guid id, CancellationToken token) => Result(await service.SetActiveAsync(User, id, false, token));
    private IActionResult Result<T>(AccountServiceResult<T> result) => result.Succeeded ? Ok(result.Value) : Problem(result.Error!);
    private IActionResult Result(AccountServiceResult result) => result.Succeeded ? NoContent() : Problem(result.Error!);
    private IActionResult Problem(AccountServiceError error) => error.ValidationErrors is null
        ? Problem(statusCode: error.StatusCode, title: error.Title, detail: error.Detail)
        : StatusCode(error.StatusCode, new ValidationProblemDetails(error.ValidationErrors.ToDictionary()) { Status = error.StatusCode, Title = error.Title, Detail = error.Detail });
}
