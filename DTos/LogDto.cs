using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record LogDto(
    long Id,
    DateTime CreatedAt,
    ActionType ActionType,
    string Reason,
    string? UserLogin,
    string? TargetUserLogin,
    string? WorkshopName,
    string? EquipmentName);
