using EnergyMeneger.DTOs;
using EnergyMeneger.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EnergyMeneger.Controllers;

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
