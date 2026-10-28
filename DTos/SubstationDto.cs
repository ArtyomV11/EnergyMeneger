using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record SubstationDto(
    int Id,
    string Name,
    double PowerLimitKw);
