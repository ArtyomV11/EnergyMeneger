namespace EnergyMeneger.Models;

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
