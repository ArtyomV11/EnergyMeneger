using EnergyMeneger.DTOs;
using EnergyMeneger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EnergyMeneger.Controllers;

[ApiController]
[Authorize(Roles = "Owner")]
[Route("api/substation")]
public sealed class SubstationController(PlantService service) : ControllerBase
{
    [HttpGet]
    public Task<SubstationDto> Get() => service.GetSubstation();

    [HttpPut]
    public Task<SubstationDto> Update(SubstationRequest request) =>
        service.UpdateSubstation(request);
}
