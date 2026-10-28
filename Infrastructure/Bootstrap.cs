using EnergyMeneger.Data;
using EnergyMeneger.Models;
using EnergyMeneger.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnergyMeneger.Infrastructure;

public static class Bootstrap
{
    public static async Task Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var login = Rules.Text(
            config["Bootstrap:Login"], "Логин владельца", 100);

        var password = config["Bootstrap:Password"];
        AuthService.ValidatePassword(password);

        await db.Database.MigrateAsync();

        Rules.Require(!await db.Factories.AnyAsync(),
            "База уже инициализирована. Повторное создание владельца запрещено.", 409);

        await using var transaction = await db.Database.BeginTransactionAsync();

        var factory = new Factory { Name = "Учебный завод" };
        db.Factories.Add(factory);
        await db.SaveChangesAsync();

        db.Substations.Add(new Substation(
            factory.Id, "Главная подстанция", 300));

        var w1 = new Workshop(
            factory.Id, "Цех №1", 70,
            SimulationType.Virtual, 30, 70);

        var w2 = new Workshop(
            factory.Id, "Цех №2", 90,
            SimulationType.Virtual, 40, 90);

        var w3 = new Workshop(
            factory.Id, "Цех №3", 140,
            SimulationType.EquipmentBased, null, null);

        db.Workshops.AddRange(w1, w2, w3);
        await db.SaveChangesAsync();

        db.Equipment.AddRange(
            new Equipment(w3.Id, "Токарный станок", 20, 45,
                EquipmentPriority.Important, true),

            new Equipment(w3.Id, "Фрезерный станок", 25, 55,
                EquipmentPriority.Important, true),

            new Equipment(w3.Id, "Сверлильный станок", 5, 15,
                EquipmentPriority.Secondary, true),

            new Equipment(w3.Id, "Компрессор", 15, 35,
                EquipmentPriority.Secondary, true),

            new Equipment(w3.Id, "Вентиляция", 5, 12,
                EquipmentPriority.Critical, false),

            new Equipment(w3.Id, "Освещение", 3, 8,
                EquipmentPriority.Critical, false)
        );

        var owner = new User
        {
            FactoryId = factory.Id,
            Login = login,
            NormalizedLogin = User.Normalize(login),
            Role = UserRole.Owner,
            Status = UserStatus.Active
        };

        owner.PasswordHash = hasher.HashPassword(owner, password!);
        db.Users.Add(owner);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        Console.WriteLine("База инициализирована. Владелец создан.");
        Console.WriteLine("Пароль в консоль не выводится.");
    }
}