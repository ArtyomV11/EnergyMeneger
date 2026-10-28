namespace EnergyMeneger.Models;

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
