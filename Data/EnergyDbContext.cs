using EnergyMeneger.Models;
using Microsoft.EntityFrameworkCore;

namespace EnergyMeneger.Data;

public sealed class EnergyDbContext : DbContext
{
    public EnergyDbContext(DbContextOptions<EnergyDbContext> options)
        : base(options) { }

    public DbSet<Factory> Factories => Set<Factory>();
    public DbSet<Substation> Substations => Set<Substation>();
    public DbSet<Workshop> Workshops => Set<Workshop>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<User> Users => Set<User>();

    public DbSet<EquipmentConsumptionHistory> EquipmentHistory =>
        Set<EquipmentConsumptionHistory>();

    public DbSet<WorkshopConsumptionHistory> WorkshopHistory =>
        Set<WorkshopConsumptionHistory>();

    public DbSet<ActionLog> ActionLogs => Set<ActionLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Factory>().ToTable("Factories");
        b.Entity<Substation>().ToTable("Substations");
        b.Entity<Workshop>().ToTable("Workshops");
        b.Entity<Equipment>().ToTable("Equipment");
        b.Entity<User>().ToTable("Users");
        b.Entity<EquipmentConsumptionHistory>().ToTable("EquipmentHistory");
        b.Entity<WorkshopConsumptionHistory>().ToTable("WorkshopHistory");
        b.Entity<ActionLog>().ToTable("ActionLogs");

        b.Entity<Factory>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Entity<Substation>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Entity<Workshop>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Entity<Equipment>().Property(x => x.Name).HasMaxLength(200).IsRequired();

        b.Entity<User>().Property(x => x.Login).HasMaxLength(100).IsRequired();
        b.Entity<User>().Property(x => x.NormalizedLogin).HasMaxLength(100).IsRequired();
        b.Entity<User>().Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired();
        b.Entity<User>().HasIndex(x => x.NormalizedLogin).IsUnique();

        b.Entity<ActionLog>().Property(x => x.Reason).HasMaxLength(2000).IsRequired();

        b.Entity<Substation>()
            .HasOne<Factory>().WithOne()
            .HasForeignKey<Substation>(x => x.FactoryId);

        b.Entity<Workshop>()
            .HasOne<Factory>().WithMany()
            .HasForeignKey(x => x.FactoryId);

        b.Entity<Equipment>()
            .HasOne<Workshop>().WithMany()
            .HasForeignKey(x => x.WorkshopId);

        b.Entity<User>()
            .HasOne<Factory>().WithMany()
            .HasForeignKey(x => x.FactoryId);

        b.Entity<User>()
            .HasOne<Workshop>().WithMany()
            .HasForeignKey(x => x.WorkshopId);

        b.Entity<EquipmentConsumptionHistory>()
            .HasOne<Equipment>().WithMany()
            .HasForeignKey(x => x.EquipmentId);

        b.Entity<WorkshopConsumptionHistory>()
            .HasOne<Workshop>().WithMany()
            .HasForeignKey(x => x.WorkshopId);

        b.Entity<ActionLog>()
            .HasOne<Factory>().WithMany()
            .HasForeignKey(x => x.FactoryId);

        b.Entity<ActionLog>()
            .HasOne<User>().WithMany()
            .HasForeignKey(x => x.UserId);

        b.Entity<ActionLog>()
            .HasOne<User>().WithMany()
            .HasForeignKey(x => x.TargetUserId);

        b.Entity<ActionLog>()
            .HasOne<Workshop>().WithMany()
            .HasForeignKey(x => x.WorkshopId);

        b.Entity<ActionLog>()
            .HasOne<Equipment>().WithMany()
            .HasForeignKey(x => x.EquipmentId);

        b.Entity<EquipmentConsumptionHistory>()
            .HasIndex(x => new { x.EquipmentId, x.RecordedAt })
            .IsUnique();

        b.Entity<WorkshopConsumptionHistory>()
            .HasIndex(x => new { x.WorkshopId, x.RecordedAt })
            .IsUnique();

        b.Entity<EquipmentConsumptionHistory>()
            .HasIndex(x => x.EquipmentId)
            .HasFilter("\"EndedAt\" IS NULL")
            .IsUnique();

        b.Entity<WorkshopConsumptionHistory>()
            .HasIndex(x => x.WorkshopId)
            .HasFilter("\"EndedAt\" IS NULL")
            .IsUnique();

        b.Entity<EquipmentConsumptionHistory>().HasIndex(x => x.EndedAt);
        b.Entity<WorkshopConsumptionHistory>().HasIndex(x => x.EndedAt);

        b.Entity<ActionLog>().HasIndex(x => new { x.FactoryId, x.CreatedAt });

        b.Entity<Equipment>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Equipment_Power",
                "\"MinPowerKw\" >= 0 AND \"MaxPowerKw\" >= \"MinPowerKw\" " +
                "AND \"MaxPowerKw\" < 'Infinity'::float8 " +
                "AND \"CurrentPowerKw\" >= 0 " +
                "AND \"CurrentPowerKw\" < 'Infinity'::float8");

            t.HasCheckConstraint("CK_Equipment_State",
                "(NOT \"IsEnabled\" AND \"CurrentPowerKw\" = 0) OR " +
                "(\"IsEnabled\" AND \"CurrentPowerKw\" BETWEEN " +
                "\"MinPowerKw\" AND \"MaxPowerKw\")");

            t.HasCheckConstraint("CK_Equipment_Priority",
                "\"Priority\" IN (1,2,3) AND " +
                "(\"Priority\" <> 1 OR NOT \"AllowAutoShutdown\")");
        });

        b.Entity<Workshop>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Workshop_Limit",
                "\"PowerLimitKw\" > 0 AND " +
                "\"PowerLimitKw\" < 'Infinity'::float8 AND " +
                "\"CurrentPowerKw\" >= 0 AND " +
                "\"CurrentPowerKw\" < 'Infinity'::float8");

            t.HasCheckConstraint("CK_Workshop_Type",
                "(\"SimulationType\" = 1 AND \"MinPowerKw\" IS NOT NULL " +
                "AND \"MaxPowerKw\" IS NOT NULL AND \"MinPowerKw\" >= 0 " +
                "AND \"MaxPowerKw\" >= \"MinPowerKw\" " +
                "AND \"MaxPowerKw\" <= \"PowerLimitKw\" " +
                "AND \"CurrentPowerKw\" BETWEEN \"MinPowerKw\" AND \"MaxPowerKw\") " +
                "OR (\"SimulationType\" = 2 AND \"MinPowerKw\" IS NULL " +
                "AND \"MaxPowerKw\" IS NULL)");
        });

        b.Entity<Substation>().ToTable(t =>
            t.HasCheckConstraint("CK_Substation_Limit",
                "\"PowerLimitKw\" > 0 AND \"PowerLimitKw\" < 'Infinity'::float8"));

        b.Entity<User>().ToTable(t =>
            t.HasCheckConstraint("CK_User_Role",
                "\"Status\" IN (1,2,3) AND " +
                "((\"Role\" = 1 AND \"WorkshopId\" IS NULL) OR " +
                "(\"Role\" IN (2,3) AND \"WorkshopId\" IS NOT NULL))"));

        b.Entity<EquipmentConsumptionHistory>().ToTable(t =>
            t.HasCheckConstraint("CK_EquipmentHistory_Interval",
                "\"PowerKw\" >= 0 AND \"PowerKw\" < 'Infinity'::float8 " +
                "AND (\"EndedAt\" IS NULL OR \"EndedAt\" >= \"RecordedAt\")"));

        b.Entity<WorkshopConsumptionHistory>().ToTable(t =>
            t.HasCheckConstraint("CK_WorkshopHistory_Interval",
                "\"PowerKw\" >= 0 AND \"PowerKw\" < 'Infinity'::float8 " +
                "AND (\"EndedAt\" IS NULL OR \"EndedAt\" >= \"RecordedAt\")"));

        foreach (var entity in b.Model.GetEntityTypes())
        {
            foreach (var fk in entity.GetForeignKeys())
                fk.DeleteBehavior = DeleteBehavior.Restrict;

            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) ||
                    property.ClrType == typeof(DateTime?))
                    property.SetColumnType("timestamp with time zone");
            }
        }
    }
}