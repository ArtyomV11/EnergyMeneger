using EnergyMeneger.DTOs;
using EnergyMeneger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EnergyMeneger.Controllers;

[ApiController]
[Authorize(Roles = "Owner")]
[Route("api/logs")]
public sealed class LogsController(PlantService service) : ControllerBase
{
    [HttpGet]
    public Task<List<LogDto>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int size = 100) =>
        service.Logs(page, size);
}
