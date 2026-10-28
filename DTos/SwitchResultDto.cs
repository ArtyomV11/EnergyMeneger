using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record SwitchResultDto(
    bool Success,
    string Message,
    EquipmentDto Equipment,
    IReadOnlyList<int> AutomaticallyDisabledIds);
