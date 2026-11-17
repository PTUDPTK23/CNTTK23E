using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditWorker.Data.Migrations
{
    /// <inheritdoc />
    public partial class NameAuditTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Processed",
                table: "Processed");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Entries",
                table: "Entries");

            migrationBuilder.RenameTable(
                name: "Processed",
                newName: "ProcessedMessages");

            migrationBuilder.RenameTable(
                name: "Entries",
                newName: "AuditLogs");

            migrationBuilder.RenameIndex(
                name: "IX_Processed_EventId",
                table: "ProcessedMessages",
                newName: "IX_ProcessedMessages_EventId");

            migrationBuilder.RenameIndex(
                name: "IX_Entries_MessageId",
                table: "AuditLogs",
                newName: "IX_AuditLogs_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcessedMessages",
                table: "ProcessedMessages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcessedMessages",
                table: "ProcessedMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "ProcessedMessages",
                newName: "Processed");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "Entries");

            migrationBuilder.RenameIndex(
                name: "IX_ProcessedMessages_EventId",
                table: "Processed",
                newName: "IX_Processed_EventId");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_MessageId",
                table: "Entries",
                newName: "IX_Entries_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Processed",
                table: "Processed",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Entries",
                table: "Entries",
                column: "Id");
        }
    }
}
