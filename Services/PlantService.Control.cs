using EnergyMeneger.DTOs;
using EnergyMeneger.Models;
using Microsoft.EntityFrameworkCore;

namespace EnergyMeneger.Services;

public sealed partial class PlantService
{
    private static double RandomPower(double min, double max) =>
        min == max ? min : min + Random.Shared.NextDouble() * (max - min);

    public static List<Equipment> SelectShutdown(
        IEnumerable<Equipment> source,
        double requiredPower)
    {
        var result = new List<Equipment>();
        var remaining = requiredPower;

        var candidates = source
            .Where(e => e.CanAutoShutdown() && e.CurrentPowerKw > 0)
            .ToList();

        foreach (var priority in new[]
                 {
                     EquipmentPriority.Secondary,
                     EquipmentPriority.Important
                 })
        {
            var level = candidates.Where(e => e.Priority == priority).ToList();

            while (remaining > 0 && level.Count > 0)
            {
                var selected = level
                    .Where(e => e.CurrentPowerKw >= remaining)
                    .OrderBy(e => e.CurrentPowerKw)
                    .ThenBy(e => e.Id)
                    .FirstOrDefault()
                    ?? level
                        .OrderByDescending(e => e.CurrentPowerKw)
                        .ThenBy(e => e.Id)
                        .First();

                result.Add(selected);
                remaining -= selected.CurrentPowerKw;
                level.Remove(selected);
            }
        }

        return result;
    }

    public Task<SwitchResultDto> Switch(int id, bool enable) =>
        Execute(async () =>
        {
            var user = await Actor();
            var e = await EquipmentById(id);
            var w = await WorkshopById(e.WorkshopId);
            Access(user, w, true);

            if (e.IsEnabled == enable)
            {
                return new SwitchResultDto(
                    true,
                    enable ? "Оборудование уже включено." : "Оборудование уже выключено.",
                    Map(e, w),
                    Array.Empty<int>());
            }

            var disabled = new List<int>();

            if (!enable)
            {
                e.Disable();

                Log(user.FactoryId, ActionType.EquipmentDisabled,
                    "Ручное выключение.", user.Id, w.Id, e.Id);
            }
            else
            {
                var station = await db.Substations
                    .SingleAsync(x => x.FactoryId == user.FactoryId);

                var workshops = await db.Workshops
                    .Where(x => x.FactoryId == user.FactoryId)
                    .ToListAsync();

                var required = Math.Max(
                    w.CurrentPowerKw + e.MaxPowerKw - w.PowerLimitKw,
                    workshops.Sum(x => x.CurrentPowerKw) +
                    e.MaxPowerKw - station.PowerLimitKw);

                var available = await db.Equipment
                    .Where(x => x.WorkshopId == w.Id && x.Id != e.Id)
                    .ToListAsync();

                var candidates = available.Where(
                    x => (int)x.Priority > (int)e.Priority);

                var plan = SelectShutdown(candidates, required);

                if (required > 0 &&
                    plan.Sum(x => x.CurrentPowerKw) < required)
                {
                    Log(user.FactoryId, ActionType.EquipmentEnableRejected,
                        $"Недостаточно мощности. Необходимо освободить " +
                        $"{required:F3} кВт.",
                        user.Id, w.Id, e.Id);

                    return new SwitchResultDto(
                        false,
                        "Недостаточно мощности. Никакие станки не отключены.",
                        Map(e, w),
                        Array.Empty<int>());
                }

                foreach (var item in plan)
                {
                    item.Disable(true);
                    disabled.Add(item.Id);

                    Log(user.FactoryId, ActionType.EquipmentAutoShutdown,
                        $"Освобождение мощности для оборудования {e.Id}.",
                        user.Id, w.Id, item.Id);
                }

                e.Enable(RandomPower(e.MinPowerKw, e.MaxPowerKw));

                Log(user.FactoryId, ActionType.EquipmentEnabled,
                    "Ручное включение.", user.Id, w.Id, e.Id);
            }

            await Recalculate(user.FactoryId);
            await Snapshot(user.FactoryId);

            return new SwitchResultDto(
                true,
                enable ? "Оборудование включено." : "Оборудование выключено.",
                Map(e, w),
                disabled);
        });

    public Task<bool> Tick(bool restart) => Execute(async () =>
    {
        var factories = await db.Factories.ToListAsync();

        foreach (var factory in factories)
        {
            var workshops = await db.Workshops
                .Where(w => w.FactoryId == factory.Id)
                .ToListAsync();

            var ids = workshops.Select(w => w.Id).ToArray();

            var equipment = await db.Equipment
                .Where(e => ids.Contains(e.WorkshopId))
                .ToListAsync();

            foreach (var w in workshops.Where(
                         w => w.SimulationType == SimulationType.Virtual))
            {
                w.SetVirtualPower(RandomPower(
                    w.MinPowerKw!.Value, w.MaxPowerKw!.Value));
            }

            foreach (var e in equipment)
            {
                e.SetPower(e.IsEnabled
                    ? RandomPower(e.MinPowerKw, e.MaxPowerKw)
                    : 0);
            }

            foreach (var w in workshops.Where(
                         w => w.SimulationType == SimulationType.EquipmentBased))
            {
                var own = equipment.Where(e => e.WorkshopId == w.Id).ToList();
                w.Recalculate(own);

                if (w.Excess() > 0)
                {
                    Log(factory.Id, ActionType.WorkshopOverload,
                        $"Превышение лимита цеха: {w.Excess():F3} кВт.",
                        workshopId: w.Id);

                    foreach (var e in SelectShutdown(own, w.Excess()))
                    {
                        e.Disable(true);

                        Log(factory.Id, ActionType.EquipmentAutoShutdown,
                            "Превышение лимита цеха.",
                            workshopId: w.Id, equipmentId: e.Id);
                    }

                    w.Recalculate(own);
                }
            }

            var station = await db.Substations
                .SingleAsync(x => x.FactoryId == factory.Id);

            var total = workshops.Sum(w => w.CurrentPowerKw);

            if (!station.CanSupply(total))
            {
                Log(factory.Id, ActionType.SubstationOverload,
                    $"Превышение лимита подстанции: {station.Excess(total):F3} кВт.");

                foreach (var e in SelectShutdown(equipment, station.Excess(total)))
                {
                    e.Disable(true);

                    Log(factory.Id, ActionType.EquipmentAutoShutdown,
                        "Превышение лимита подстанции.",
                        workshopId: e.WorkshopId, equipmentId: e.Id);
                }

                foreach (var w in workshops.Where(
                             w => w.SimulationType == SimulationType.EquipmentBased))
                {
                    w.Recalculate(
                        equipment.Where(e => e.WorkshopId == w.Id).ToList());
                }
            }

            foreach (var w in workshops.Where(w => w.Excess() > 0))
            {
                Log(factory.Id, ActionType.CriticalOverload,
                    $"Перегрузка цеха не устранена: {w.Excess():F3} кВт.",
                    workshopId: w.Id);
            }

            total = workshops.Sum(w => w.CurrentPowerKw);

            if (!station.CanSupply(total))
            {
                Log(factory.Id, ActionType.CriticalOverload,
                    $"Перегрузка подстанции не устранена: " +
                    $"{station.Excess(total):F3} кВт.");
            }

            if (restart)
                Log(factory.Id, ActionType.SimulationStarted,
                    "Начат новый сеанс моделирования.");

            await Snapshot(factory.Id, restart);
        }

        return true;
    });

    public Task<bool> Cleanup() => Execute(async () =>
    {
        var cutoff = Rules.Now().AddYears(-3);

        await db.EquipmentHistory
            .Where(x => x.EndedAt != null && x.EndedAt < cutoff)
            .ExecuteDeleteAsync();

        await db.WorkshopHistory
            .Where(x => x.EndedAt != null && x.EndedAt < cutoff)
            .ExecuteDeleteAsync();

        return true;
    });
}