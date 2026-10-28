using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record FactorySummaryDto(
    int FactoryId,
    string FactoryName,
    double PowerLimitKw,
    double CurrentPowerKw,
    double ExcessKw,
    DateTime? LastObservedAt,
    IReadOnlyList<WorkshopDto> Workshops);
