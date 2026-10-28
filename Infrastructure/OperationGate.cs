namespace EnergyMeneger.Infrastructure;

public sealed class OperationGate
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
}