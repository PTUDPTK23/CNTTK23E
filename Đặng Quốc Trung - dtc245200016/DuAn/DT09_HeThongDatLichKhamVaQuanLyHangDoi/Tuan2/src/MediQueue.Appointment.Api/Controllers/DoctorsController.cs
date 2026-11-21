using MediQueue.Appointment.Api.Services;
using MediQueue.Appointment.Api.Services.Profiles;
using MediQueue.Contracts.Profiles;
using MediQueue.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediQueue.Appointment.Api.Controllers;

[ApiController, Route("api/doctors"), Authorize]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class DoctorsController(IDoctorManagementService service) : ControllerBase
{
    [HttpGet, Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> Index([FromQuery] DoctorListQuery query, CancellationToken token) => Result(await service.GetAsync(query, token));

    [HttpGet("{id:guid}"), Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> Details(Guid id, CancellationToken token) => Result(await service.GetAsync(id, token));

    [HttpGet("create-options"), Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> CreateOptions(CancellationToken token) => Result(await service.GetCreateOptionsAsync(token));

    [HttpPost, Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> Create(CreateDoctorProfileRequest request, CancellationToken token)
    {
        AccountServiceResult<DoctorDetailsResponse> result = await service.CreateAsync(User, request, token);
        return result.Succeeded ? CreatedAtAction(nameof(Details), new { id = result.Value!.Id }, result.Value) : Problem(result.Error!);
    }

    [HttpPut("{id:guid}"), Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> Update(Guid id, UpdateDoctorProfileRequest request, CancellationToken token) => Result(await service.UpdateAsync(User, id, request, token));

    [HttpGet("me"), Authorize(Roles = SystemRoles.Doctor)]
    public async Task<IActionResult> Me(CancellationToken token) => Result(await service.GetSelfAsync(User, token));

    private IActionResult Result<T>(AccountServiceResult<T> result) => result.Succeeded ? Ok(result.Value) : Problem(result.Error!);
    private IActionResult Problem(AccountServiceError error) => error.ValidationErrors is null
        ? Problem(statusCode: error.StatusCode, title: error.Title, detail: error.Detail)
        : StatusCode(error.StatusCode, new ValidationProblemDetails(error.ValidationErrors.ToDictionary()) { Status = error.StatusCode, Title = error.Title, Detail = error.Detail });
}
