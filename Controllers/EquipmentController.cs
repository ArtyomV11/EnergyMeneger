using EnergyMeneger.DTOs;
using EnergyMeneger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EnergyMeneger.Controllers;

[ApiController]
[Authorize]
[Route("api/equipment")]
public sealed class EquipmentController(PlantService service) : ControllerBase
{
    [HttpGet]
    public Task<List<EquipmentDto>> GetAll(
        [FromQuery] int? workshopId,
        [FromQuery] bool? enabled) =>
        service.GetEquipment(workshopId, enabled);

    [HttpGet("{id:int}")]
    public Task<EquipmentDto> Get(int id) => service.GetEquipmentItem(id);

    [Authorize(Roles = "Owner")]
    [HttpPost]
    public async Task<ActionResult<EquipmentDto>> Create(EquipmentRequest request)
    {
        var dto = await service.SaveEquipment(null, request);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [Authorize(Roles = "Owner")]
    [HttpPut("{id:int}")]
    public Task<EquipmentDto> Update(int id, EquipmentRequest request) =>
        service.SaveEquipment(id, request);

    [Authorize(Roles = "Owner")]
    [HttpDelete("{id:int}")]
    public Task<MessageDto> Delete(int id) => service.DeleteEquipment(id);

    [Authorize(Roles = "Owner,Foreman")]
    [HttpPost("{id:int}/enable")]
    public async Task<ActionResult<SwitchResultDto>> Enable(int id)
    {
        var dto = await service.Switch(id, true);
        return dto.Success ? Ok(dto) : Conflict(dto);
    }

    [Authorize(Roles = "Owner,Foreman")]
    [HttpPost("{id:int}/disable")]
    public async Task<ActionResult<SwitchResultDto>> Disable(int id)
    {
        var dto = await service.Switch(id, false);
        return Ok(dto);
    }
}
