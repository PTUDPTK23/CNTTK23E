using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DashboardService.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EventVersionProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Registrations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Events");
        }
    }
}
