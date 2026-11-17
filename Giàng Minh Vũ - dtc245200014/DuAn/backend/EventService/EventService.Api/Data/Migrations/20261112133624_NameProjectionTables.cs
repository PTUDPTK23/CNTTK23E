using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventService.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NameProjectionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Outbox",
                table: "Outbox");

            migrationBuilder.RenameTable(
                name: "Outbox",
                newName: "OutboxMessages");

            migrationBuilder.RenameIndex(
                name: "IX_Outbox_PublishedAt_CreatedAt",
                table: "OutboxMessages",
                newName: "IX_OutboxMessages_PublishedAt_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OutboxMessages",
                table: "OutboxMessages",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OutboxMessages",
                table: "OutboxMessages");

            migrationBuilder.RenameTable(
                name: "OutboxMessages",
                newName: "Outbox");

            migrationBuilder.RenameIndex(
                name: "IX_OutboxMessages_PublishedAt_CreatedAt",
                table: "Outbox",
                newName: "IX_Outbox_PublishedAt_CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Outbox",
                table: "Outbox",
                column: "Id");
        }
    }
}
