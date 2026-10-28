using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record StatisticsDto(
    string Scope,
    int ObjectId,
    DateTime From,
    DateTime To,
    double EnergyKwh,
    double? MinimumPowerKw,
    double? MaximumPowerKw,
    double? AveragePowerKw,
    double CoveredSeconds,
    double MissingSeconds);
