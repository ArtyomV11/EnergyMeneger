using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record UserDto(
    int Id,
    string Login,
    UserRole Role,
    UserStatus Status,
    int? WorkshopId,
    string? WorkshopName);
