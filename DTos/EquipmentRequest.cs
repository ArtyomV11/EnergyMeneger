using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record EquipmentRequest(
    int WorkshopId,
    string Name,
    double MinPowerKw,
    double MaxPowerKw,
    EquipmentPriority Priority,
    bool AllowAutoShutdown);
