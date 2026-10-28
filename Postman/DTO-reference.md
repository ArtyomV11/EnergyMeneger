# DTO из main

Commit: 26736fc7c5818ec494c610f3d52c6b3e5efad20c

JSON: camelCase, enum — строки (Program.cs, JsonStringEnumConverter). Ниже точные C# контракты; nullable поля передаются как null.

## CreateUserRequest.cs

```csharp
public sealed record CreateUserRequest(
    string Login,
    string Password,
    UserRole Role,
    int WorkshopId);
```

## CredentialsDto.cs

```csharp
public sealed record CredentialsDto(
    UserDto User,
    string InitialPassword);
```

## EquipmentDto.cs

```csharp
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
```

## EquipmentRequest.cs

```csharp
public sealed record EquipmentRequest(
    int WorkshopId,
    string Name,
    double MinPowerKw,
    double MaxPowerKw,
    EquipmentPriority Priority,
    bool AllowAutoShutdown);
```

## ErrorDto.cs

```csharp
public sealed record ErrorDto(string Message);
```

## FactorySummaryDto.cs

```csharp
public sealed record FactorySummaryDto(
    int FactoryId,
    string FactoryName,
    double PowerLimitKw,
    double CurrentPowerKw,
    double ExcessKw,
    DateTime? LastObservedAt,
    IReadOnlyList<WorkshopDto> Workshops);
```

## HistoryPointDto.cs

```csharp
public sealed record HistoryPointDto(
    long Id,
    int ObjectId,
    string ObjectName,
    string WorkshopName,
    DateTime RecordedAt,
    DateTime? EndedAt,
    double PowerKw);
```

## LogDto.cs

```csharp
public sealed record LogDto(
    long Id,
    DateTime CreatedAt,
    ActionType ActionType,
    string Reason,
    string? UserLogin,
    string? TargetUserLogin,
    string? WorkshopName,
    string? EquipmentName);
```

## LoginRequest.cs

```csharp
public sealed record LoginRequest(string Login, string Password);
```

## MessageDto.cs

```csharp
public sealed record MessageDto(string Message);
```

## StatisticsDto.cs

```csharp
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
```

## SubstationDto.cs

```csharp
public sealed record SubstationDto(
    int Id,
    string Name,
    double PowerLimitKw);
```

## SubstationRequest.cs

```csharp
public sealed record SubstationRequest(
    string Name,
    double PowerLimitKw);
```

## SwitchResultDto.cs

```csharp
public sealed record SwitchResultDto(
    bool Success,
    string Message,
    EquipmentDto Equipment,
    IReadOnlyList<int> AutomaticallyDisabledIds);
```

## TokenDto.cs

```csharp
public sealed record TokenDto(
    string AccessToken,
    DateTime ExpiresAt,
    UserDto User);
```

## UpdateUserRequest.cs

```csharp
public sealed record UpdateUserRequest(
    UserRole Role,
    int WorkshopId,
    UserStatus Status);
```

## UserDto.cs

```csharp
public sealed record UserDto(
    int Id,
    string Login,
    UserRole Role,
    UserStatus Status,
    int? WorkshopId,
    string? WorkshopName);
```

## WorkshopDto.cs

```csharp
public sealed record WorkshopDto(
    int Id,
    string Name,
    double PowerLimitKw,
    SimulationType SimulationType,
    double? MinPowerKw,
    double? MaxPowerKw,
    double CurrentPowerKw);
```

## WorkshopRequest.cs

```csharp
public sealed record WorkshopRequest(
    string Name,
    double PowerLimitKw,
    SimulationType SimulationType,
    double? MinPowerKw,
    double? MaxPowerKw);
```

