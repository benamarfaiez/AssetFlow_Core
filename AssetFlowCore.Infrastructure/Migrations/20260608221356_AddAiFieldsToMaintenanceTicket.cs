using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlowCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFieldsToMaintenanceTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "assistance_note",
                table: "t_maintenance_tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_ai_processing",
                table: "t_maintenance_tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "assistance_note",
                table: "t_maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "is_ai_processing",
                table: "t_maintenance_tickets");
        }
    }
}
