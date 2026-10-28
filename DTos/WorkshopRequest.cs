using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record WorkshopRequest(
    string Name,
    double PowerLimitKw,
    SimulationType SimulationType,
    double? MinPowerKw,
    double? MaxPowerKw);
