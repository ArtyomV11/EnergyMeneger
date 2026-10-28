namespace EnergyMeneger.Models;

public enum SimulationType
{
    Virtual = 1,
    EquipmentBased = 2
}

public enum EquipmentPriority
{
    Critical = 1,
    Important = 2,
    Secondary = 3
}

public enum UserRole
{
    Owner = 1,
    Foreman = 2,
    Worker = 3
}

public enum UserStatus
{
    Active = 1,
    Blocked = 2,
    Dismissed = 3
}

public enum ActionType
{
    EquipmentEnabled = 1,
    EquipmentDisabled = 2,
    EquipmentAutoShutdown = 3,
    EquipmentEnableRejected = 4,
    WorkshopOverload = 5,
    SubstationOverload = 6,
    CriticalOverload = 7,
    SettingsChanged = 8,
    UserCreated = 9,
    UserChanged = 10,
    EquipmentCreated = 11,
    EquipmentDeleted = 12,
    WorkshopCreated = 13,
    WorkshopDeleted = 14,
    SimulationStarted = 15
}

public sealed class ApiException : Exception
{
    public int StatusCode { get; }

    public ApiException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

public static class Rules
{
    public static void Require(
        bool condition,
        string message,
        int statusCode = 400)
    {
        if (!condition)
            throw new ApiException(statusCode, message);
    }

    public static string Text(string? value, string field, int max = 200)
    {
        Require(!string.IsNullOrWhiteSpace(value), $"Заполните поле «{field}».");

        var result = value!.Trim();

        Require(result.Length <= max,
            $"Поле «{field}» не должно превышать {max} символов.");

        return result;
    }

    public static void Power(double value)
    {
        Require(double.IsFinite(value) && value >= 0,
            "Мощность должна быть конечным неотрицательным числом.");
    }

    public static void Limit(double value)
    {
        Power(value);
        Require(value > 0, "Лимит мощности должен быть больше нуля.");
    }

    public static void Range(double min, double max)
    {
        Power(min);
        Power(max);
        Require(min <= max, "Минимальная мощность превышает максимальную.");
    }

    public static void EnumValue<T>(T value) where T : struct, Enum
    {
        Require(Enum.IsDefined(typeof(T), value),
            "Недопустимое значение перечисления.");
    }

    public static DateTime Now()
    {
        var ticks = DateTime.UtcNow.Ticks;
        return new DateTime(ticks - ticks % 10, DateTimeKind.Utc);
    }
}

public sealed class Factory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime? LastObservedAt { get; set; }
}

public sealed class Substation
{
    public int Id { get; private set; }
    public int FactoryId { get; private set; }
    public string Name { get; private set; } = "";
    public double PowerLimitKw { get; private set; }

    private Substation() { }

    public Substation(int factoryId, string name, double limit)
    {
        FactoryId = factoryId;
        Name = Rules.Text(name, "Название подстанции");
        Rules.Limit(limit);
        PowerLimitKw = limit;
    }

    public bool CanSupply(double power)
    {
        Rules.Power(power);
        return power <= PowerLimitKw;
    }

    public double Excess(double power)
    {
        Rules.Power(power);
        return Math.Max(0, power - PowerLimitKw);
    }

    public void Update(
        string name,
        double limit,
        double workshopLimits,
        double currentPower)
    {
        var checkedName = Rules.Text(name, "Название подстанции");
        Rules.Limit(limit);

        Rules.Require(workshopLimits <= limit,
            "Лимит подстанции меньше суммы лимитов цехов.", 409);

        Rules.Require(currentPower <= limit,
            "Лимит подстанции меньше текущего потребления.", 409);

        Name = checkedName;
        PowerLimitKw = limit;
    }
}

public sealed class Workshop
{
    public int Id { get; private set; }
    public int FactoryId { get; private set; }
    public string Name { get; private set; } = "";
    public double PowerLimitKw { get; private set; }
    public SimulationType SimulationType { get; private set; }
    public double? MinPowerKw { get; private set; }
    public double? MaxPowerKw { get; private set; }
    public double CurrentPowerKw { get; private set; }

    private Workshop() { }

    public Workshop(
        int factoryId,
        string name,
        double limit,
        SimulationType type,
        double? min,
        double? max)
    {
        FactoryId = factoryId;
        SimulationType = type;
        Validate(limit, type, min, max);
        Name = Rules.Text(name, "Название цеха");
        PowerLimitKw = limit;
        MinPowerKw = min;
        MaxPowerKw = max;
        CurrentPowerKw = type == SimulationType.Virtual ? min!.Value : 0;
    }

    public bool CanSupply(double power)
    {
        Rules.Power(power);
        return power <= PowerLimitKw;
    }

    public double Excess() => Math.Max(0, CurrentPowerKw - PowerLimitKw);

    public void SetVirtualPower(double power)
    {
        Rules.Require(SimulationType == SimulationType.Virtual,
            "Цех не является виртуальным.");

        Rules.Power(power);

        Rules.Require(power >= MinPowerKw && power <= MaxPowerKw,
            "Мощность находится вне диапазона виртуального цеха.");

        CurrentPowerKw = power;
    }

    public void Recalculate(IReadOnlyCollection<Equipment> equipment)
    {
        Rules.Require(SimulationType == SimulationType.EquipmentBased,
            "В виртуальном цехе нет отдельных устройств.");

        Rules.Require(equipment.All(x => x.WorkshopId == Id),
            "В расчёт передано оборудование другого цеха.");

        var power = equipment
            .Where(x => x.IsEnabled)
            .Sum(x => x.CurrentPowerKw);

        Rules.Power(power);
        CurrentPowerKw = power;
    }

    public void Update(
        string name,
        double limit,
        double? min,
        double? max)
    {
        var checkedName = Rules.Text(name, "Название цеха");
        Validate(limit, SimulationType, min, max);

        Rules.Require(CurrentPowerKw <= limit,
            "Новый лимит ниже текущего потребления цеха.", 409);

        Name = checkedName;
        PowerLimitKw = limit;
        MinPowerKw = min;
        MaxPowerKw = max;


        if (SimulationType == SimulationType.Virtual)
            CurrentPowerKw = Math.Clamp(CurrentPowerKw, min!.Value, max!.Value);
    }

    private static void Validate(
        double limit,
        SimulationType type,
        double? min,
        double? max)
    {
        Rules.Limit(limit);
        Rules.EnumValue(type);

        if (type == SimulationType.Virtual)
        {
            Rules.Require(min.HasValue && max.HasValue,
                "Укажите диапазон мощности виртуального цеха.");

            Rules.Range(min!.Value, max!.Value);

            Rules.Require(max.Value <= limit,
                "Максимальная мощность виртуального цеха превышает лимит.");
        }
        else
        {
            Rules.Require(min == null && max == null,
                "У цеха с оборудованием диапазон должен быть null.");
        }
    }
}

public sealed class Equipment
{
    public int Id { get; private set; }
    public int WorkshopId { get; private set; }
    public string Name { get; private set; } = "";
    public double MinPowerKw { get; private set; }
    public double MaxPowerKw { get; private set; }
    public double CurrentPowerKw { get; private set; }
    public bool IsEnabled { get; private set; }
    public EquipmentPriority Priority { get; private set; }
    public bool AllowAutoShutdown { get; private set; }

    private Equipment() { }

    public Equipment(
        int workshopId,
        string name,
        double min,
        double max,
        EquipmentPriority priority,
        bool autoShutdown)
    {
        WorkshopId = workshopId;
        Update(name, min, max, priority, autoShutdown);
    }

    public bool CanAutoShutdown() =>
        IsEnabled &&
        AllowAutoShutdown &&
        Priority != EquipmentPriority.Critical;

    public void Enable(double initialPower)
    {
        Rules.Require(!IsEnabled, "Оборудование уже включено.", 409);
        ValidatePower(initialPower);
        IsEnabled = true;
        CurrentPowerKw = initialPower;
    }

    public void Disable(bool automatically = false)
    {
        Rules.Require(IsEnabled, "Оборудование уже выключено.", 409);

        if (automatically)
            Rules.Require(CanAutoShutdown(),
                "Автоматическое отключение запрещено.", 409);

        IsEnabled = false;
        CurrentPowerKw = 0;
    }

    public void SetPower(double power)
    {
        if (!IsEnabled)
        {
            Rules.Require(power == 0,
                "Выключенное оборудование должно потреблять 0 кВт.");

            CurrentPowerKw = 0;
            return;
        }

        ValidatePower(power);
        CurrentPowerKw = power;
    }

    public void Update(
        string name,
        double min,
        double max,
        EquipmentPriority priority,
        bool autoShutdown)
    {
        var checkedName = Rules.Text(name, "Название оборудования");
        Rules.Range(min, max);
        Rules.EnumValue(priority);

        Rules.Require(
            priority != EquipmentPriority.Critical || !autoShutdown,
            "Критическое оборудование нельзя отключать автоматически.");

        Rules.Require(
            !IsEnabled || (CurrentPowerKw >= min && CurrentPowerKw <= max),
            "Сначала выключите оборудование: текущая мощность " +
            "не входит в новый диапазон.", 409);

        Name = checkedName;
        MinPowerKw = min;
        MaxPowerKw = max;
        Priority = priority;
        AllowAutoShutdown = autoShutdown;
    }

    private void ValidatePower(double power)
    {
        Rules.Power(power);
        Rules.Require(power >= MinPowerKw && power <= MaxPowerKw,
            "Мощность оборудования находится вне диапазона.");
    }
}

public sealed class User
{
    public int Id { get; set; }
    public int FactoryId { get; set; }
    public int? WorkshopId { get; set; }
    public string Login { get; set; } = "";
    public string NormalizedLogin { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }

    public static string Normalize(string login) =>
        Rules.Text(login, "Логин", 100).ToUpperInvariant();
}

public sealed class EquipmentConsumptionHistory
{
    public long Id { get; set; }
    public int EquipmentId { get; set; }
    public DateTime RecordedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public double PowerKw { get; set; }
}

public sealed class WorkshopConsumptionHistory
{
    public long Id { get; set; }
    public int WorkshopId { get; set; }
    public DateTime RecordedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public double PowerKw { get; set; }
}

public sealed class ActionLog
{
    public long Id { get; set; }
    public int FactoryId { get; set; }
    public int? UserId { get; set; }
    public int? TargetUserId { get; set; }
    public int? WorkshopId { get; set; }
    public int? EquipmentId { get; set; }
    public ActionType ActionType { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}