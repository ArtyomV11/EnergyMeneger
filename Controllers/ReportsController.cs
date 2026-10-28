using EnergyMeneger.DTOs;
using EnergyMeneger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EnergyMeneger.Controllers;

[ApiController]
[Authorize(Roles = "Owner")]
[Route("api/reports")]
public sealed class ReportsController(PlantService service) : ControllerBase
{
    [HttpGet("summary")]
    public Task<FactorySummaryDto> Summary() => service.Summary();

    [HttpGet("history")]
    public Task<List<HistoryPointDto>> History(
        [FromQuery] string scope,
        [FromQuery] int id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int page = 1,
        [FromQuery] int size = 200) =>
        service.History(scope, id, from, to, page, size);

    [HttpGet("statistics")]
    public Task<StatisticsDto> Statistics(
        [FromQuery] string scope,
        [FromQuery] int id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to) =>
        service.Statistics(scope, id, from, to);
}
