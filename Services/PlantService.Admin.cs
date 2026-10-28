using EnergyMeneger.DTOs;
using EnergyMeneger.Models;
using Microsoft.EntityFrameworkCore;

namespace EnergyMeneger.Services;

public sealed partial class PlantService
{
    public Task<List<WorkshopDto>> GetWorkshops() => Execute(async () =>
    {
        var user = await Actor();

        return await db.Workshops
            .Where(w => w.FactoryId == user.FactoryId &&
                (user.Role == UserRole.Owner || w.Id == user.WorkshopId))
            .OrderBy(w => w.Id)
            .Select(w => new WorkshopDto(
                w.Id, w.Name, w.PowerLimitKw, w.SimulationType,
                w.MinPowerKw, w.MaxPowerKw, w.CurrentPowerKw))
            .ToListAsync();
    });

    public Task<WorkshopDto> GetWorkshop(int id) => Execute(async () =>
    {
        var user = await Actor();
        var w = await WorkshopById(id);
        Access(user, w);
        return Map(w);
    });

    public Task<WorkshopDto> SaveWorkshop(int? id, WorkshopRequest r) =>
        Execute(async () =>
        {
            var user = await Actor(true);

            var station = await db.Substations
                .SingleAsync(x => x.FactoryId == user.FactoryId);

            var workshops = await db.Workshops
                .Where(x => x.FactoryId == user.FactoryId)
                .ToListAsync();

            Rules.Limit(r.PowerLimitKw);

            var allocated = workshops
                .Where(x => !id.HasValue || x.Id != id.Value)
                .Sum(x => x.PowerLimitKw);

            Rules.Require(allocated + r.PowerLimitKw <= station.PowerLimitKw,
                "Сумма лимитов цехов превысит лимит подстанции.", 409);

            Workshop w;

            if (id.HasValue)
            {
                w = await WorkshopById(id.Value);
                Access(user, w);

                Rules.Require(w.SimulationType == r.SimulationType,
                    "Тип моделирования существующего цеха менять нельзя.");

                w.Update(r.Name, r.PowerLimitKw, r.MinPowerKw, r.MaxPowerKw);

                Log(user.FactoryId, ActionType.SettingsChanged,
                    $"Изменены настройки цеха {w.Id}: {w.Name}.",
                    user.Id, w.Id);
            }
            else
            {
                w = new Workshop(
                    user.FactoryId, r.Name, r.PowerLimitKw,
                    r.SimulationType, r.MinPowerKw, r.MaxPowerKw);

                db.Workshops.Add(w);
                await db.SaveChangesAsync();

                Log(user.FactoryId, ActionType.WorkshopCreated,
                    $"Создан цех {w.Id}: {w.Name}.", user.Id);
            }

            await Snapshot(user.FactoryId);
            return Map(w);
        });

    public Task<MessageDto> DeleteWorkshop(int id) => Execute(async () =>
    {
        var user = await Actor(true);
        var w = await WorkshopById(id);
        Access(user, w);

        var used =
            await db.Equipment.AnyAsync(x => x.WorkshopId == id) ||
            await db.Users.AnyAsync(x => x.WorkshopId == id) ||
            await db.WorkshopHistory.AnyAsync(x => x.WorkshopId == id) ||
            await db.ActionLogs.AnyAsync(x => x.WorkshopId == id);

        Rules.Require(!used,
            "Нельзя удалить цех с оборудованием, сотрудниками или историей.", 409);

        db.Workshops.Remove(w);

        Log(user.FactoryId, ActionType.WorkshopDeleted,
            $"Удалён цех {id}: {w.Name}.", user.Id);

        return new MessageDto("Цех удалён.");
    });

    public Task<SubstationDto> GetSubstation() => Execute(async () =>
    {
        var user = await Actor(true);

        var s = await db.Substations
            .SingleAsync(x => x.FactoryId == user.FactoryId);

        return new SubstationDto(s.Id, s.Name, s.PowerLimitKw);
    });

    public Task<SubstationDto> UpdateSubstation(SubstationRequest r) =>
        Execute(async () =>
        {
            var user = await Actor(true);

            var s = await db.Substations
                .SingleAsync(x => x.FactoryId == user.FactoryId);

            var workshops = await db.Workshops
                .Where(x => x.FactoryId == user.FactoryId)
                .ToListAsync();

            s.Update(
                r.Name, r.PowerLimitKw,
                workshops.Sum(x => x.PowerLimitKw),
                workshops.Sum(x => x.CurrentPowerKw));

            Log(user.FactoryId, ActionType.SettingsChanged,
                $"Лимит подстанции установлен: {s.PowerLimitKw} кВт.",
                user.Id);

            return new SubstationDto(s.Id, s.Name, s.PowerLimitKw);
        });

    public Task<EquipmentDto> SaveEquipment(int? id, EquipmentRequest r) =>
        Execute(async () =>
        {
            var user = await Actor(true);
            var w = await WorkshopById(r.WorkshopId);
            Access(user, w);

            Rules.Require(w.SimulationType == SimulationType.EquipmentBased,
                "В виртуальном цехе нельзя создавать отдельное оборудование.");

            Equipment e;

            if (id.HasValue)
            {
                e = await EquipmentById(id.Value);

                var oldWorkshop = await WorkshopById(e.WorkshopId);
                Access(user, oldWorkshop);

                Rules.Require(e.WorkshopId == r.WorkshopId,
                    "Перемещение оборудования между цехами не поддерживается.");

                e.Update(
                    r.Name, r.MinPowerKw, r.MaxPowerKw,
                    r.Priority, r.AllowAutoShutdown);

                Log(user.FactoryId, ActionType.SettingsChanged,
                    $"Изменены настройки оборудования {e.Id}: {e.Name}.",
                    user.Id, w.Id, e.Id);
            }
            else
            {
                e = new Equipment(
                    w.Id, r.Name, r.MinPowerKw, r.MaxPowerKw,
                    r.Priority, r.AllowAutoShutdown);

                db.Equipment.Add(e);
                await db.SaveChangesAsync();

                Log(user.FactoryId, ActionType.EquipmentCreated,
                    $"Создано оборудование {e.Id}: {e.Name}.",
                    user.Id, w.Id);
            }

            return Map(e, w);
        });

    public Task<MessageDto> DeleteEquipment(int id) => Execute(async () =>
    {
        var user = await Actor(true);
        var e = await EquipmentById(id);
        var w = await WorkshopById(e.WorkshopId);
        Access(user, w);

        Rules.Require(!e.IsEnabled,
            "Сначала выключите оборудование.", 409);

        var used =
            await db.EquipmentHistory.AnyAsync(x => x.EquipmentId == id) ||
            await db.ActionLogs.AnyAsync(x => x.EquipmentId == id);

        Rules.Require(!used,
            "Оборудование имеет историю или события. Удаление запрещено.", 409);

        db.Equipment.Remove(e);

        Log(user.FactoryId, ActionType.EquipmentDeleted,
            $"Удалено оборудование {id}: {e.Name}.", user.Id, w.Id);

        return new MessageDto("Оборудование удалено.");
    });

    public Task<List<UserDto>> GetUsers() => Execute(async () =>
    {
        var user = await Actor(true);

        return await (
            from u in db.Users
            join w in db.Workshops on u.WorkshopId equals (int?)w.Id into groupW
            from w in groupW.DefaultIfEmpty()
            where u.FactoryId == user.FactoryId
            orderby u.Id
            select new UserDto(
                u.Id, u.Login, u.Role, u.Status, u.WorkshopId,
                w == null ? null : w.Name)
        ).ToListAsync();
    });

    public Task<CredentialsDto> CreateUser(CreateUserRequest r) =>
        Execute(async () =>
        {
            var actor = await Actor(true);

            Rules.Require(r.Role is UserRole.Foreman or UserRole.Worker,
                "Здесь создаются только начальники цехов и рабочие.");

            var w = await WorkshopById(r.WorkshopId);
            Access(actor, w);

            AuthService.ValidatePassword(r.Password);
            var login = Rules.Text(r.Login, "Логин", 100);
            var normalized = User.Normalize(login);

            Rules.Require(
                !await db.Users.AnyAsync(x => x.NormalizedLogin == normalized),
                "Этот логин уже занят.", 409);

            var user = new User
            {
                FactoryId = actor.FactoryId,
                WorkshopId = w.Id,
                Login = login,
                NormalizedLogin = normalized,
                Role = r.Role,
                Status = UserStatus.Active
            };

            user.PasswordHash = hasher.HashPassword(user, r.Password);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            Log(actor.FactoryId, ActionType.UserCreated,
                $"Создан сотрудник {user.Login}.",
                actor.Id, targetUserId: user.Id);

            return new CredentialsDto(
                new UserDto(
                    user.Id, user.Login, user.Role, user.Status,
                    w.Id, w.Name),
                r.Password);
        });

    public Task<UserDto> UpdateUser(int id, UpdateUserRequest r) =>
        Execute(async () =>
        {
            var actor = await Actor(true);

            var user = await db.Users.SingleOrDefaultAsync(
                x => x.Id == id && x.FactoryId == actor.FactoryId)
                ?? throw new ApiException(404, "Сотрудник не найден.");

            Rules.Require(user.Role != UserRole.Owner,
                "Учётная запись владельца этим методом не изменяется.", 403);

            Rules.Require(user.Status != UserStatus.Dismissed,
                "Уволенного сотрудника нельзя изменять.", 409);

            Rules.Require(r.Role is UserRole.Foreman or UserRole.Worker,
                "Допустимы роли Foreman и Worker.");

            Rules.EnumValue(r.Status);

            var w = await WorkshopById(r.WorkshopId);
            Access(actor, w);

            user.Role = r.Role;
            user.WorkshopId = w.Id;
            user.Status = r.Status;

            Log(actor.FactoryId, ActionType.UserChanged,
                $"Сотрудник {user.Login}: роль {r.Role}, статус {r.Status}, " +
                $"цех {w.Name}.",
                actor.Id, targetUserId: user.Id);

            return new UserDto(
                user.Id, user.Login, user.Role,
                user.Status, w.Id, w.Name);
        });
}