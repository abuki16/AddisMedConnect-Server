using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddisMedConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAmbulanceModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ambulances_Users_AssignedDriverId",
                table: "Ambulances");

            migrationBuilder.DropIndex(
                name: "IX_Ambulances_AssignedDriverId",
                table: "Ambulances");

            migrationBuilder.DropColumn(
                name: "AssignedDriverId",
                table: "Ambulances");

            migrationBuilder.DropColumn(
                name: "CurrentLatitude",
                table: "Ambulances");

            migrationBuilder.DropColumn(
                name: "CurrentLongitude",
                table: "Ambulances");

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "Ambulances",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Ambulances",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "Ambulances");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Ambulances");

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedDriverId",
                table: "Ambulances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLatitude",
                table: "Ambulances",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CurrentLongitude",
                table: "Ambulances",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ambulances_AssignedDriverId",
                table: "Ambulances",
                column: "AssignedDriverId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ambulances_Users_AssignedDriverId",
                table: "Ambulances",
                column: "AssignedDriverId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
