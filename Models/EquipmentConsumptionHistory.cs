namespace EnergyMeneger.Models;

public sealed class EquipmentConsumptionHistory
{
    public long Id { get; set; }
    public int EquipmentId { get; set; }
    public DateTime RecordedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public double PowerKw { get; set; }
}
