using EnergyMeneger.DTOs;
using EnergyMeneger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EnergyMeneger.Controllers;

[ApiController]
[Authorize]
[Route("api/workshops")]
public sealed class WorkshopsController(PlantService service) : ControllerBase
{
    [HttpGet]
    public Task<List<WorkshopDto>> GetAll() => service.GetWorkshops();

    [HttpGet("{id:int}")]
    public Task<WorkshopDto> Get(int id) => service.GetWorkshop(id);

    [Authorize(Roles = "Owner")]
    [HttpPost]
    public async Task<ActionResult<WorkshopDto>> Create(WorkshopRequest request)
    {
        var dto = await service.SaveWorkshop(null, request);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [Authorize(Roles = "Owner")]
    [HttpPut("{id:int}")]
    public Task<WorkshopDto> Update(int id, WorkshopRequest request) =>
        service.SaveWorkshop(id, request);

    [Authorize(Roles = "Owner")]
    [HttpDelete("{id:int}")]
    public Task<MessageDto> Delete(int id) => service.DeleteWorkshop(id);
}
