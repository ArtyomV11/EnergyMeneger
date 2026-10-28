using EnergyMeneger.DTOs;
using EnergyMeneger.Models;
using Microsoft.EntityFrameworkCore;

namespace EnergyMeneger.Services;

public sealed partial class PlantService
{
    public Task<List<EquipmentDto>> GetEquipment(
        int? workshopId,
        bool? enabled) => Execute(async () =>
        {
            var user = await Actor();

            if (workshopId.HasValue)
                Access(user, await WorkshopById(workshopId.Value));

 
            var query = db.Equipment.Join(
                db.Workshops,
                e => e.WorkshopId,
                w => w.Id,
                (e, w) => new { E = e, W = w });

            return await query
                .Where(x => x.W.FactoryId == user.FactoryId)
                .Where(x => user.Role == UserRole.Owner ||
                            x.W.Id == user.WorkshopId)
                .Where(x => !workshopId.HasValue || x.W.Id == workshopId.Value)
                .Where(x => !enabled.HasValue || x.E.IsEnabled == enabled.Value)
                .OrderBy(x => x.E.Id)
                .Select(x => new EquipmentDto(
                    x.E.Id, x.W.Id, x.W.Name, x.E.Name,
                    x.E.MinPowerKw, x.E.MaxPowerKw, x.E.CurrentPowerKw,
                    x.E.IsEnabled, x.E.Priority, x.E.AllowAutoShutdown))
                .ToListAsync();
        });

    public Task<EquipmentDto> GetEquipmentItem(int id) => Execute(async () =>
    {
        var user = await Actor();
        var e = await EquipmentById(id);
        var w = await WorkshopById(e.WorkshopId);
        Access(user, w);
        return Map(e, w);
    });

    public Task<FactorySummaryDto> Summary() => Execute(async () =>
    {
        var user = await Actor(true);


        var header = await (
            from f in db.Factories
            join s in db.Substations on f.Id equals s.FactoryId
            where f.Id == user.FactoryId
            select new
            {
                f.Id,
                f.Name,
                f.LastObservedAt,
                s.PowerLimitKw
            }).SingleAsync();

        var workshops = await db.Workshops
            .Where(w => w.FactoryId == user.FactoryId)
            .OrderBy(w => w.Id)
            .Select(w => new WorkshopDto(
                w.Id, w.Name, w.PowerLimitKw, w.SimulationType,
                w.MinPowerKw, w.MaxPowerKw, w.CurrentPowerKw))
            .ToListAsync();

        var total = workshops.Sum(w => w.CurrentPowerKw);

        return new FactorySummaryDto(
            header.Id, header.Name, header.PowerLimitKw,
            total, Math.Max(0, total - header.PowerLimitKw),
            header.LastObservedAt, workshops);
    });

    private static void Period(DateTime from, DateTime to)
    {
        Rules.Require(
            from.Kind == DateTimeKind.Utc && to.Kind == DateTimeKind.Utc,
            "Передавайте время в UTC с окончанием Z.");

        Rules.Require(to > from, "Конец периода должен быть позже начала.");

        Rules.Require(to - from <= TimeSpan.FromDays(31),
            "Один запрос может охватывать не более 31 дня.");

        Rules.Require(from >= Rules.Now().AddYears(-3),
            "Начало периода находится за пределами трёхлетней истории.");
    }

    private static void Page(int page, int size)
    {
        Rules.Require(page >= 1 && page <= 100000,
            "Номер страницы должен быть от 1 до 100000.");

        Rules.Require(size >= 1 && size <= 1000,
            "Размер страницы должен быть от 1 до 1000.");
    }

    public Task<List<HistoryPointDto>> History(
        string scope,
        int id,
        DateTime from,
        DateTime to,
        int page,
        int size) => Execute(async () =>
        {
            var user = await Actor(true);
            Period(from, to);
            Page(page, size);

            if (scope == "equipment")
            {
                var e = await EquipmentById(id);
                Access(user, await WorkshopById(e.WorkshopId));

                return await (
                    from h in db.EquipmentHistory
                    join item in db.Equipment on h.EquipmentId equals item.Id
                    join w in db.Workshops on item.WorkshopId equals w.Id
                    where item.Id == id &&
                          h.RecordedAt < to &&
                          (h.EndedAt == null || h.EndedAt > @from)
                    orderby h.RecordedAt, h.Id
                    select new HistoryPointDto(
                        h.Id, item.Id, item.Name, w.Name,
                        h.RecordedAt, h.EndedAt, h.PowerKw)
                ).Skip((page - 1) * size).Take(size).ToListAsync();
            }

            Rules.Require(scope == "workshop",
                "Для истории допустимы scope=equipment или scope=workshop.");

            var workshop = await WorkshopById(id);
            Access(user, workshop);

            return await db.WorkshopHistory
                .Where(h => h.WorkshopId == id &&
                            h.RecordedAt < to &&
                            (h.EndedAt == null || h.EndedAt > @from))
                .OrderBy(h => h.RecordedAt)
                .ThenBy(h => h.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(h => new HistoryPointDto(
                    h.Id, id, workshop.Name, workshop.Name,
                    h.RecordedAt, h.EndedAt, h.PowerKw))
                .ToListAsync();
        });

    private sealed record Interval(DateTime Start, DateTime End, double Power);

    public Task<StatisticsDto> Statistics(
        string scope,
        int id,
        DateTime from,
        DateTime to) => Execute(async () =>
        {
            var user = await Actor(true);
            Period(from, to);

            var factory = await db.Factories
                .SingleAsync(x => x.Id == user.FactoryId);

            var checkpoint = factory.LastObservedAt ?? from;
            List<Interval> intervals;

            if (scope == "equipment")
            {
                var e = await EquipmentById(id);
                Access(user, await WorkshopById(e.WorkshopId));

                var rows = await db.EquipmentHistory.AsNoTracking()
                    .Where(h => h.EquipmentId == id &&
                                h.RecordedAt < to &&
                                (h.EndedAt == null || h.EndedAt > from))
                    .Select(h => new
                    {
                        h.RecordedAt,
                        h.EndedAt,
                        h.PowerKw
                    }).ToListAsync();

                intervals = rows.Select(h => new Interval(
                    h.RecordedAt, h.EndedAt ?? checkpoint, h.PowerKw)).ToList();
            }
            else
            {
                Rules.Require(scope is "workshop" or "factory",
                    "Допустимы scope=equipment, workshop, factory.");

                if (scope == "workshop")
                    Access(user, await WorkshopById(id));
                else
                    Rules.Require(id == user.FactoryId,
                        "Нет доступа к предприятию.", 403);

                var rows = await (
                    from h in db.WorkshopHistory.AsNoTracking()
                    join w in db.Workshops on h.WorkshopId equals w.Id
                    where w.FactoryId == user.FactoryId &&
                          (scope == "factory" || w.Id == id) &&
                          h.RecordedAt < to &&
                          (h.EndedAt == null || h.EndedAt > @from)
                    select new
                    {
                        h.RecordedAt,
                        h.EndedAt,
                        h.PowerKw
                    }).ToListAsync();


                intervals = rows
                    .GroupBy(h => new
                    {
                        Start = h.RecordedAt,
                        End = h.EndedAt ?? checkpoint
                    })
                    .Select(g => new Interval(
                        g.Key.Start, g.Key.End, g.Sum(x => x.PowerKw)))
                    .ToList();
            }

            double energy = 0;
            double covered = 0;
            double? min = null;
            double? max = null;

            foreach (var interval in intervals)
            {
                var start = interval.Start < from ? from : interval.Start;
                var end = interval.End > to ? to : interval.End;

                if (end <= start)
                    continue;

                var seconds = (end - start).TotalSeconds;

                energy += interval.Power * seconds / 3600.0;
                covered += seconds;
                min = min.HasValue ? Math.Min(min.Value, interval.Power) : interval.Power;
                max = max.HasValue ? Math.Max(max.Value, interval.Power) : interval.Power;
            }

            var requested = (to - from).TotalSeconds;

            return new StatisticsDto(
                scope, id, from, to, energy, min, max,
                covered > 0 ? energy * 3600 / covered : null,
                covered,
                Math.Max(0, requested - covered));
        });

    public Task<List<LogDto>> Logs(int page, int size) => Execute(async () =>
    {
        var user = await Actor(true);
        Page(page, size);

        return await (
            from log in db.ActionLogs
            join actor in db.Users on log.UserId equals (int?)actor.Id into actors
            from actor in actors.DefaultIfEmpty()
            join target in db.Users on log.TargetUserId equals (int?)target.Id into targets
            from target in targets.DefaultIfEmpty()
            join workshop in db.Workshops on log.WorkshopId equals (int?)workshop.Id into workshops
            from workshop in workshops.DefaultIfEmpty()
            join equipment in db.Equipment on log.EquipmentId equals (int?)equipment.Id into devices
            from equipment in devices.DefaultIfEmpty()
            where log.FactoryId == user.FactoryId
            orderby log.CreatedAt descending, log.Id descending
            select new LogDto(
                log.Id, log.CreatedAt, log.ActionType, log.Reason,
                actor == null ? null : actor.Login,
                target == null ? null : target.Login,
                workshop == null ? null : workshop.Name,
                equipment == null ? null : equipment.Name)
        ).Skip((page - 1) * size).Take(size).ToListAsync();
    });
}