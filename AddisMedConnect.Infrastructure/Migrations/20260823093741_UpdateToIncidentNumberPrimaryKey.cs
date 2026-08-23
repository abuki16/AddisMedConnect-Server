using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddisMedConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToIncidentNumberPrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Beds_EmergencyCases_CurrentCaseId",
                table: "Beds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmergencyCases",
                table: "EmergencyCases");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "EmergencyCases");

            migrationBuilder.RenameColumn(
                name: "ChiefComplaint",
                table: "EmergencyCases",
                newName: "IncidentReason");

            migrationBuilder.AlterColumn<string>(
                name: "CurrentCaseId",
                table: "Beds",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmergencyCases",
                table: "EmergencyCases",
                column: "IncidentNumber");

            migrationBuilder.AddForeignKey(
                name: "FK_Beds_EmergencyCases_CurrentCaseId",
                table: "Beds",
                column: "CurrentCaseId",
                principalTable: "EmergencyCases",
                principalColumn: "IncidentNumber",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Beds_EmergencyCases_CurrentCaseId",
                table: "Beds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EmergencyCases",
                table: "EmergencyCases");

            migrationBuilder.RenameColumn(
                name: "IncidentReason",
                table: "EmergencyCases",
                newName: "ChiefComplaint");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "EmergencyCases",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "CurrentCaseId",
                table: "Beds",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_EmergencyCases",
                table: "EmergencyCases",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Beds_EmergencyCases_CurrentCaseId",
                table: "Beds",
                column: "CurrentCaseId",
                principalTable: "EmergencyCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
