using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record ErrorDto(string Message);
public sealed record MessageDto(string Message);

public sealed record LoginRequest(string Login, string Password);

public sealed record TokenDto(
    string AccessToken,
    DateTime ExpiresAt,
    UserDto User);

public sealed record UserDto(
    int Id,
    string Login,
    UserRole Role,
    UserStatus Status,
    int? WorkshopId,
    string? WorkshopName);

public sealed record CreateUserRequest(
    string Login,
    string Password,
    UserRole Role,
    int WorkshopId);

public sealed record UpdateUserRequest(
    UserRole Role,
    int WorkshopId,
    UserStatus Status);

public sealed record CredentialsDto(
    UserDto User,
    string InitialPassword);

public sealed record WorkshopRequest(
    string Name,
    double PowerLimitKw,
    SimulationType SimulationType,
    double? MinPowerKw,
    double? MaxPowerKw);

public sealed record WorkshopDto(
    int Id,
    string Name,
    double PowerLimitKw,
    SimulationType SimulationType,
    double? MinPowerKw,
    double? MaxPowerKw,
    double CurrentPowerKw);

public sealed record EquipmentRequest(
    int WorkshopId,
    string Name,
    double MinPowerKw,
    double MaxPowerKw,
    EquipmentPriority Priority,
    bool AllowAutoShutdown);

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

public sealed record SubstationRequest(
    string Name,
    double PowerLimitKw);

public sealed record SubstationDto(
    int Id,
    string Name,
    double PowerLimitKw);

public sealed record SwitchResultDto(
    bool Success,
    string Message,
    EquipmentDto Equipment,
    IReadOnlyList<int> AutomaticallyDisabledIds);

public sealed record FactorySummaryDto(
    int FactoryId,
    string FactoryName,
    double PowerLimitKw,
    double CurrentPowerKw,
    double ExcessKw,
    DateTime? LastObservedAt,
    IReadOnlyList<WorkshopDto> Workshops);

public sealed record HistoryPointDto(
    long Id,
    int ObjectId,
    string ObjectName,
    string WorkshopName,
    DateTime RecordedAt,
    DateTime? EndedAt,
    double PowerKw);

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

public sealed record LogDto(
    long Id,
    DateTime CreatedAt,
    ActionType ActionType,
    string Reason,
    string? UserLogin,
    string? TargetUserLogin,
    string? WorkshopName,
    string? EquipmentName);