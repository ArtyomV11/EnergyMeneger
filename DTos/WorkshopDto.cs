using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record WorkshopDto(
    int Id,
    string Name,
    double PowerLimitKw,
    SimulationType SimulationType,
    double? MinPowerKw,
    double? MaxPowerKw,
    double CurrentPowerKw);
