using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EnergyMeneger.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Factories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastObservedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Substations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FactoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PowerLimitKw = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Substations", x => x.Id);
                    table.CheckConstraint("CK_Substation_Limit", "\"PowerLimitKw\" > 0 AND \"PowerLimitKw\" < 'Infinity'::float8");
                    table.ForeignKey(
                        name: "FK_Substations_Factories_FactoryId",
                        column: x => x.FactoryId,
                        principalTable: "Factories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Workshops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FactoryId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PowerLimitKw = table.Column<double>(type: "double precision", nullable: false),
                    SimulationType = table.Column<int>(type: "integer", nullable: false),
                    MinPowerKw = table.Column<double>(type: "double precision", nullable: true),
                    MaxPowerKw = table.Column<double>(type: "double precision", nullable: true),
                    CurrentPowerKw = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workshops", x => x.Id);
                    table.CheckConstraint("CK_Workshop_Limit", "\"PowerLimitKw\" > 0 AND \"PowerLimitKw\" < 'Infinity'::float8 AND \"CurrentPowerKw\" >= 0 AND \"CurrentPowerKw\" < 'Infinity'::float8");
                    table.CheckConstraint("CK_Workshop_Type", "(\"SimulationType\" = 1 AND \"MinPowerKw\" IS NOT NULL AND \"MaxPowerKw\" IS NOT NULL AND \"MinPowerKw\" >= 0 AND \"MaxPowerKw\" >= \"MinPowerKw\" AND \"MaxPowerKw\" <= \"PowerLimitKw\" AND \"CurrentPowerKw\" BETWEEN \"MinPowerKw\" AND \"MaxPowerKw\") OR (\"SimulationType\" = 2 AND \"MinPowerKw\" IS NULL AND \"MaxPowerKw\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_Workshops_Factories_FactoryId",
                        column: x => x.FactoryId,
                        principalTable: "Factories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkshopId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MinPowerKw = table.Column<double>(type: "double precision", nullable: false),
                    MaxPowerKw = table.Column<double>(type: "double precision", nullable: false),
                    CurrentPowerKw = table.Column<double>(type: "double precision", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    AllowAutoShutdown = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                    table.CheckConstraint("CK_Equipment_Power", "\"MinPowerKw\" >= 0 AND \"MaxPowerKw\" >= \"MinPowerKw\" AND \"MaxPowerKw\" < 'Infinity'::float8 AND \"CurrentPowerKw\" >= 0 AND \"CurrentPowerKw\" < 'Infinity'::float8");
                    table.CheckConstraint("CK_Equipment_Priority", "\"Priority\" IN (1,2,3) AND (\"Priority\" <> 1 OR NOT \"AllowAutoShutdown\")");
                    table.CheckConstraint("CK_Equipment_State", "(NOT \"IsEnabled\" AND \"CurrentPowerKw\" = 0) OR (\"IsEnabled\" AND \"CurrentPowerKw\" BETWEEN \"MinPowerKw\" AND \"MaxPowerKw\")");
                    table.ForeignKey(
                        name: "FK_Equipment_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FactoryId = table.Column<int>(type: "integer", nullable: false),
                    WorkshopId = table.Column<int>(type: "integer", nullable: true),
                    Login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedLogin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_User_Role", "\"Status\" IN (1,2,3) AND ((\"Role\" = 1 AND \"WorkshopId\" IS NULL) OR (\"Role\" IN (2,3) AND \"WorkshopId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_Users_Factories_FactoryId",
                        column: x => x.FactoryId,
                        principalTable: "Factories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkshopHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkshopId = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PowerKw = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkshopHistory", x => x.Id);
                    table.CheckConstraint("CK_WorkshopHistory_Interval", "\"PowerKw\" >= 0 AND \"PowerKw\" < 'Infinity'::float8 AND (\"EndedAt\" IS NULL OR \"EndedAt\" >= \"RecordedAt\")");
                    table.ForeignKey(
                        name: "FK_WorkshopHistory_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PowerKw = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentHistory", x => x.Id);
                    table.CheckConstraint("CK_EquipmentHistory_Interval", "\"PowerKw\" >= 0 AND \"PowerKw\" < 'Infinity'::float8 AND (\"EndedAt\" IS NULL OR \"EndedAt\" >= \"RecordedAt\")");
                    table.ForeignKey(
                        name: "FK_EquipmentHistory_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FactoryId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    TargetUserId = table.Column<int>(type: "integer", nullable: true),
                    WorkshopId = table.Column<int>(type: "integer", nullable: true),
                    EquipmentId = table.Column<int>(type: "integer", nullable: true),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionLogs_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActionLogs_Factories_FactoryId",
                        column: x => x.FactoryId,
                        principalTable: "Factories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActionLogs_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActionLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActionLogs_Workshops_WorkshopId",
                        column: x => x.WorkshopId,
                        principalTable: "Workshops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_EquipmentId",
                table: "ActionLogs",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_FactoryId_CreatedAt",
                table: "ActionLogs",
                columns: new[] { "FactoryId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_TargetUserId",
                table: "ActionLogs",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_UserId",
                table: "ActionLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_WorkshopId",
                table: "ActionLogs",
                column: "WorkshopId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_WorkshopId",
                table: "Equipment",
                column: "WorkshopId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentHistory_EndedAt",
                table: "EquipmentHistory",
                column: "EndedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentHistory_EquipmentId",
                table: "EquipmentHistory",
                column: "EquipmentId",
                unique: true,
                filter: "\"EndedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentHistory_EquipmentId_RecordedAt",
                table: "EquipmentHistory",
                columns: new[] { "EquipmentId", "RecordedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Substations_FactoryId",
                table: "Substations",
                column: "FactoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FactoryId",
                table: "Users",
                column: "FactoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedLogin",
                table: "Users",
                column: "NormalizedLogin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_WorkshopId",
                table: "Users",
                column: "WorkshopId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopHistory_EndedAt",
                table: "WorkshopHistory",
                column: "EndedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopHistory_WorkshopId",
                table: "WorkshopHistory",
                column: "WorkshopId",
                unique: true,
                filter: "\"EndedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkshopHistory_WorkshopId_RecordedAt",
                table: "WorkshopHistory",
                columns: new[] { "WorkshopId", "RecordedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workshops_FactoryId",
                table: "Workshops",
                column: "FactoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionLogs");

            migrationBuilder.DropTable(
                name: "EquipmentHistory");

            migrationBuilder.DropTable(
                name: "Substations");

            migrationBuilder.DropTable(
                name: "WorkshopHistory");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "Workshops");

            migrationBuilder.DropTable(
                name: "Factories");
        }
    }
}
