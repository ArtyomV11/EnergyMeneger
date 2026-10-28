namespace EnergyMeneger.Models;

public sealed class Factory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime? LastObservedAt { get; set; }
}
