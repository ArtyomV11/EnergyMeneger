namespace EnergyMeneger.Models;

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
