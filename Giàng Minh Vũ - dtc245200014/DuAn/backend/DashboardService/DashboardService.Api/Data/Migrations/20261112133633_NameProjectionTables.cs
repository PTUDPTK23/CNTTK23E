using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DashboardService.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NameProjectionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Registrations",
                table: "Registrations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Processed",
                table: "Processed");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Events",
                table: "Events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Activities",
                table: "Activities");

            migrationBuilder.RenameTable(
                name: "Registrations",
                newName: "DashboardRegistrations");

            migrationBuilder.RenameTable(
                name: "Processed",
                newName: "ProcessedMessages");

            migrationBuilder.RenameTable(
                name: "Events",
                newName: "DashboardStatistics");

            migrationBuilder.RenameTable(
                name: "Activities",
                newName: "DashboardActivities");

            migrationBuilder.RenameIndex(
                name: "IX_Processed_EventId",
                table: "ProcessedMessages",
                newName: "IX_ProcessedMessages_EventId");

            migrationBuilder.RenameIndex(
                name: "IX_Activities_MessageId",
                table: "DashboardActivities",
                newName: "IX_DashboardActivities_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DashboardRegistrations",
                table: "DashboardRegistrations",
                column: "RegistrationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessedMessages",
                table: "ProcessedMessages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DashboardStatistics",
                table: "DashboardStatistics",
                column: "EventId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DashboardActivities",
                table: "DashboardActivities",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessedMessages",
                table: "ProcessedMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DashboardStatistics",
                table: "DashboardStatistics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DashboardRegistrations",
                table: "DashboardRegistrations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DashboardActivities",
                table: "DashboardActivities");

            migrationBuilder.RenameTable(
                name: "ProcessedMessages",
                newName: "Processed");

            migrationBuilder.RenameTable(
                name: "DashboardStatistics",
                newName: "Events");

            migrationBuilder.RenameTable(
                name: "DashboardRegistrations",
                newName: "Registrations");

            migrationBuilder.RenameTable(
                name: "DashboardActivities",
                newName: "Activities");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessedMessages_EventId",
                table: "Processed",
                newName: "IX_Processed_EventId");

            migrationBuilder.RenameIndex(
                name: "IX_DashboardActivities_MessageId",
                table: "Activities",
                newName: "IX_Activities_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Processed",
                table: "Processed",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Events",
                table: "Events",
                column: "EventId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Registrations",
                table: "Registrations",
                column: "RegistrationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Activities",
                table: "Activities",
                column: "Id");
        }
    }
}
