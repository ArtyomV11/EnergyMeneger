namespace EnergyMeneger.Models;

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
        Rules.Require(limit == PlantLayout.SubstationLimitKw,
            "Лимит подстанции фиксирован: 300 кВт.", 409);
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
        Rules.Require(limit == PlantLayout.SubstationLimitKw,
            "Лимит подстанции фиксирован: 300 кВт.", 409);

        Rules.Require(workshopLimits <= limit,
            "Лимит подстанции меньше суммы лимитов цехов.", 409);

        Rules.Require(currentPower <= limit,
            "Лимит подстанции меньше текущего потребления.", 409);

        Name = checkedName;
        PowerLimitKw = limit;
    }
}
