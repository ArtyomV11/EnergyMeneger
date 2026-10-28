using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record UpdateUserRequest(
    UserRole Role,
    int WorkshopId,
    UserStatus Status);
