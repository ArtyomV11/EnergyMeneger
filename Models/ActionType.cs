namespace EnergyMeneger.Models;

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
