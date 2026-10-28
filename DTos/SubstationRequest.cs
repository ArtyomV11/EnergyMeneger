using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record SubstationRequest(
    string Name,
    double PowerLimitKw);
