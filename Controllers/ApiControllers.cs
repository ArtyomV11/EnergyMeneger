using EnergyMeneger.DTOs;
using EnergyMeneger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EnergyMeneger.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService service) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public Task<TokenDto> Login(LoginRequest request) =>
        service.Login(request);
}

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

[ApiController]
[Authorize(Roles = "Owner")]
[Route("api/users")]
public sealed class UsersController(PlantService service) : ControllerBase
{
    [HttpGet]
    public Task<List<UserDto>> GetAll() => service.GetUsers();

    [HttpPost]
    public async Task<ActionResult<CredentialsDto>> Create(CreateUserRequest request)
    {
        var dto = await service.CreateUser(request);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpPut("{id:int}")]
    public Task<UserDto> Update(int id, UpdateUserRequest request) =>
        service.UpdateUser(id, request);
}

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