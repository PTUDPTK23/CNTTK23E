using MediQueue.Contracts.Health;
using Microsoft.AspNetCore.Mvc;

namespace MediQueue.Appointment.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ServiceStatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<ServiceStatusResponse> Get()
    {
        return Ok(new ServiceStatusResponse("MediQueue.Appointment.Api", "Healthy"));
    }
}
