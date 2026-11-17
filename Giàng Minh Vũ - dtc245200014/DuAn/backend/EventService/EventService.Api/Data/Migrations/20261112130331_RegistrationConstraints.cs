using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventService.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RegistrationConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Registrations_EventId_ParticipantId_Status",
                table: "Registrations");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Participants",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId_ParticipantId",
                table: "Registrations",
                columns: new[] { "EventId", "ParticipantId" },
                unique: true,
                filter: "[Status] <> N'Cancelled'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_Counts",
                table: "Events",
                sql: "[Capacity]>0 AND [RegisteredCount]>=0 AND [RegisteredCount]<=[Capacity] AND [CheckedInCount]>=0 AND [CheckedInCount]<=[RegisteredCount]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Registrations_EventId_ParticipantId",
                table: "Registrations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_Counts",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Participants",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(320)",
                oldMaxLength: 320);

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId_ParticipantId_Status",
                table: "Registrations",
                columns: new[] { "EventId", "ParticipantId", "Status" });
        }
    }
}
