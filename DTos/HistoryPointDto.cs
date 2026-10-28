using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record HistoryPointDto(
    long Id,
    int ObjectId,
    string ObjectName,
    string WorkshopName,
    DateTime RecordedAt,
    DateTime? EndedAt,
    double PowerKw);
