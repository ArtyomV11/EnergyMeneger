using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record EquipmentDto(
    int Id,
    int WorkshopId,
    string WorkshopName,
    string Name,
    double MinPowerKw,
    double MaxPowerKw,
    double CurrentPowerKw,
    bool IsEnabled,
    EquipmentPriority Priority,
    bool AllowAutoShutdown);
