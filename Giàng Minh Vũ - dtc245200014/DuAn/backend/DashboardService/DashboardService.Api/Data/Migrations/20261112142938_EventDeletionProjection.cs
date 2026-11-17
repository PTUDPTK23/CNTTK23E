using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DashboardService.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class EventDeletionProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DashboardStatistics",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DashboardStatistics");
        }
    }
}
