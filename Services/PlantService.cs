using System.Security.Claims;
using EnergyMeneger.Data;
using EnergyMeneger.DTOs;
using EnergyMeneger.Infrastructure;
using EnergyMeneger.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnergyMeneger.Services;

public sealed partial class PlantService(
    EnergyDbContext db,
    OperationGate gate,
    IHttpContextAccessor http,
    IPasswordHasher<User> hasher)
{
    private async Task<User> Actor(bool owner = false)
    {
        var value = http.HttpContext?.User.FindFirstValue("uid");

        Rules.Require(int.TryParse(value, out var id),
            "Требуется авторизация.", 401);

        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id);

        Rules.Require(user != null && user.Status == UserStatus.Active,
            "Учётная запись недоступна.", 401);

        Rules.Require(!owner || user!.Role == UserRole.Owner,
            "Доступ разрешён только владельцу.", 403);

        return user!;
    }

    private static void Access(User user, Workshop workshop, bool control = false)
    {
        Rules.Require(user.FactoryId == workshop.FactoryId,
            "Нет доступа к предприятию.", 403);

        Rules.Require(
            user.Role == UserRole.Owner ||
            user.WorkshopId == workshop.Id,
            "Нет доступа к этому цеху.", 403);

        Rules.Require(!control || user.Role != UserRole.Worker,
            "Рабочий не может управлять оборудованием.", 403);
    }

    private async Task<T> Execute<T>(Func<Task<T>> action)
    {
        await gate.Semaphore.WaitAsync();

        try
        {
            await using var transaction =
                await db.Database.BeginTransactionAsync();

            var result = await action();

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return result;
        }
        finally
        {
            gate.Semaphore.Release();
        }
    }

    private async Task<Workshop> WorkshopById(int id)
    {
        return await db.Workshops.SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new ApiException(404, "Цех не найден.");
    }

    private async Task<Equipment> EquipmentById(int id)
    {
        return await db.Equipment.SingleOrDefaultAsync(x => x.Id == id)
            ?? throw new ApiException(404, "Оборудование не найдено.");
    }

    private static WorkshopDto Map(Workshop w) => new(
        w.Id, w.Name, w.PowerLimitKw, w.SimulationType,
        w.MinPowerKw, w.MaxPowerKw, w.CurrentPowerKw);

    private static EquipmentDto Map(Equipment e, Workshop w) => new(
        e.Id, e.WorkshopId, w.Name, e.Name,
        e.MinPowerKw, e.MaxPowerKw, e.CurrentPowerKw,
        e.IsEnabled, e.Priority, e.AllowAutoShutdown);

    private void Log(
        int factoryId,
        ActionType type,
        string reason,
        int? userId = null,
        int? workshopId = null,
        int? equipmentId = null,
        int? targetUserId = null)
    {
        db.ActionLogs.Add(new ActionLog
        {
            FactoryId = factoryId,
            ActionType = type,
            Reason = reason,
            CreatedAt = Rules.Now(),
            UserId = userId,
            WorkshopId = workshopId,
            EquipmentId = equipmentId,
            TargetUserId = targetUserId
        });
    }

    private async Task Recalculate(int factoryId)
    {
        var workshops = await db.Workshops
            .Where(w => w.FactoryId == factoryId)
            .ToListAsync();

        var equipment = await db.Equipment
            .Where(e => db.Workshops.Any(
                w => w.Id == e.WorkshopId && w.FactoryId == factoryId))
            .ToListAsync();

        foreach (var w in workshops.Where(
                     w => w.SimulationType == SimulationType.EquipmentBased))
        {
            w.Recalculate(equipment.Where(e => e.WorkshopId == w.Id).ToList());
        }
    }

    private async Task Snapshot(int factoryId, bool restart = false)
    {
        var factory = await db.Factories.SingleAsync(x => x.Id == factoryId);
        var now = Rules.Now();

        if (factory.LastObservedAt.HasValue && now <= factory.LastObservedAt)
            now = factory.LastObservedAt.Value.AddTicks(10);

        var workshops = await db.Workshops
            .Where(x => x.FactoryId == factoryId)
            .ToListAsync();

        var ids = workshops.Select(x => x.Id).ToArray();

        var equipment = await db.Equipment
            .Where(x => ids.Contains(x.WorkshopId))
            .ToListAsync();

        var equipmentIds = equipment.Select(x => x.Id).ToArray();

        var oldWorkshops = await db.WorkshopHistory
            .Where(x => ids.Contains(x.WorkshopId) && x.EndedAt == null)
            .ToListAsync();

        var oldEquipment = await db.EquipmentHistory
            .Where(x => equipmentIds.Contains(x.EquipmentId) && x.EndedAt == null)
            .ToListAsync();

        // После перезапуска не продлеваем историю через время простоя.
        var closeAt = restart
            ? factory.LastObservedAt ?? now
            : now;

        foreach (var row in oldWorkshops)
            row.EndedAt = closeAt < row.RecordedAt ? row.RecordedAt : closeAt;

        foreach (var row in oldEquipment)
            row.EndedAt = closeAt < row.RecordedAt ? row.RecordedAt : closeAt;


        await db.SaveChangesAsync();

        db.WorkshopHistory.AddRange(workshops.Select(w =>
            new WorkshopConsumptionHistory
            {
                WorkshopId = w.Id,
                RecordedAt = now,
                PowerKw = w.CurrentPowerKw
            }));

        db.EquipmentHistory.AddRange(equipment.Select(e =>
            new EquipmentConsumptionHistory
            {
                EquipmentId = e.Id,
                RecordedAt = now,
                PowerKw = e.CurrentPowerKw
            }));

        factory.LastObservedAt = now;
    }
}