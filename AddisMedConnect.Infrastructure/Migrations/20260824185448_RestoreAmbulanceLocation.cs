using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AddisMedConnect.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestoreAmbulanceLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLatitude",
                table: "Ambulances");

            migrationBuilder.DropColumn(
                name: "CurrentLongitude",
                table: "Ambulances");
        }
    }
}
