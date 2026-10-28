using EnergyMeneger.Services;

namespace EnergyMeneger.Infrastructure;

public sealed class SimulationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SimulationWorker> logger) : BackgroundService
{
    private DateTime lastCleanup = DateTime.MinValue;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
     
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<PlantService>();

        await service.Tick(true);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<PlantService>();

                await service.Tick(false);

                if (DateTime.UtcNow.Date > lastCleanup.Date)
                {
                    await service.Cleanup();
                    lastCleanup = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogCritical(exception,
                    "Моделирование остановлено из-за ошибки. " +
                    "Приложение будет остановлено, чтобы не продолжать " +
                    "работу с недостоверной историей.");

                throw;
            }
        }
    }
}