namespace EnergyMeneger.Models;

public sealed class WorkshopConsumptionHistory
{
    public long Id { get; set; }
    public int WorkshopId { get; set; }
    public DateTime RecordedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public double PowerKw { get; set; }
}
