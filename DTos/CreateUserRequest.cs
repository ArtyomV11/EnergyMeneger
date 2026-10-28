using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record CreateUserRequest(
    string Login,
    string Password,
    UserRole Role,
    int WorkshopId);
