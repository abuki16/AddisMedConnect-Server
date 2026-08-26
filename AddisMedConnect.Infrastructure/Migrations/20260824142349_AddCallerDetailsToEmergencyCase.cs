using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddisMedConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCallerDetailsToEmergencyCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CallerName",
                table: "EmergencyCases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CallerPhone",
                table: "EmergencyCases",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CallerName",
                table: "EmergencyCases");

            migrationBuilder.DropColumn(
                name: "CallerPhone",
                table: "EmergencyCases");
        }
    }
}
