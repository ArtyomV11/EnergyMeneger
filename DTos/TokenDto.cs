using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record TokenDto(
    string AccessToken,
    DateTime ExpiresAt,
    UserDto User);
