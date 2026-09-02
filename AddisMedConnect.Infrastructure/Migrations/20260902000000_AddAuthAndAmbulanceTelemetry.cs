using System;
using AddisMedConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddisMedConnect.Infrastructure.Migrations;

[DbContext(typeof(AddisDbContext))]
[Migration("20260902000000_AddAuthAndAmbulanceTelemetry")]
public partial class AddAuthAndAmbulanceTelemetry : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "DriverUserId", table: "Ambulances", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "LastLocationUpdatedAt", table: "Ambulances", type: "timestamp with time zone", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Ambulances_DriverUserId", table: "Ambulances", column: "DriverUserId");
        migrationBuilder.CreateIndex(name: "IX_Ambulances_PlateNumber", table: "Ambulances", column: "PlateNumber", unique: true);
        migrationBuilder.AddForeignKey(name: "FK_Ambulances_Users_DriverUserId", table: "Ambulances", column: "DriverUserId", principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
        migrationBuilder.CreateTable(
            name: "AmbulanceLocations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AmbulanceId = table.Column<Guid>(type: "uuid", nullable: false),
                Latitude = table.Column<double>(type: "double precision", nullable: false),
                Longitude = table.Column<double>(type: "double precision", nullable: false),
                AddressLabel = table.Column<string>(type: "text", nullable: true),
                IncidentNumber = table.Column<string>(type: "text", nullable: true),
                RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AmbulanceLocations", x => x.Id);
                table.ForeignKey(name: "FK_AmbulanceLocations_Ambulances_AmbulanceId", column: x => x.AmbulanceId, principalTable: "Ambulances", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(name: "IX_AmbulanceLocations_AmbulanceId_RecordedAt", table: "AmbulanceLocations", columns: new[] { "AmbulanceId", "RecordedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AmbulanceLocations");
        migrationBuilder.DropForeignKey(name: "FK_Ambulances_Users_DriverUserId", table: "Ambulances");
        migrationBuilder.DropIndex(name: "IX_Ambulances_DriverUserId", table: "Ambulances");
        migrationBuilder.DropIndex(name: "IX_Ambulances_PlateNumber", table: "Ambulances");
        migrationBuilder.DropColumn(name: "DriverUserId", table: "Ambulances");
        migrationBuilder.DropColumn(name: "LastLocationUpdatedAt", table: "Ambulances");
    }
}
