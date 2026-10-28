using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EnergyMeneger.Data;
using EnergyMeneger.DTOs;
using EnergyMeneger.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EnergyMeneger.Services;

public sealed class AuthService(
    EnergyDbContext db,
    IConfiguration configuration,
    IPasswordHasher<User> hasher)
{
    public static void ValidatePassword(string? password, string field = "Пароль")
    {
        Rules.Require(
            password is { Length: >= 12 and <= 128 },
            $"{field} должен содержать от 12 до 128 символов.");
    }

    public async Task<TokenDto> Login(LoginRequest request)
    {
        var login = User.Normalize(request.Login);

        Rules.Require(
            request.Password is { Length: > 0 and <= 128 },
            "Неверный логин или пароль.", 401);

        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.NormalizedLogin == login);

        if (user == null ||
            user.Status != UserStatus.Active ||
            hasher.VerifyHashedPassword(
                user, user.PasswordHash, request.Password)
            == PasswordVerificationResult.Failed)
        {
            throw new ApiException(401, "Неверный логин или пароль.");
        }

        var expires = DateTime.UtcNow.AddHours(2);

        var claims = new[]
        {
            new Claim("uid", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256));

        var workshopName = user.WorkshopId == null
            ? null
            : await db.Workshops
                .Where(x => x.Id == user.WorkshopId)
                .Select(x => x.Name)
                .SingleAsync();

        return new TokenDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            expires,
            new UserDto(
                user.Id, user.Login, user.Role, user.Status,
                user.WorkshopId, workshopName));
    }

    public async Task<ClaimsPrincipal?> ValidatePrincipal(
        ClaimsPrincipal? principal)
    {
        if (!int.TryParse(principal?.FindFirstValue("uid"), out var id))
            return null;

        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id);

        if (user == null || user.Status != UserStatus.Active)
            return null;

        // Роль берём из БД, а не из потенциально устаревшего токена.
        return new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim("uid", user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Login),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            },
            "Bearer"));
    }
}
