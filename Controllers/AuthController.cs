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
