using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlowCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HistoriserMotifTransfert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_ticket_transfer_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    maintenance_ticket_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    from_team_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    to_team_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    transferred_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_ticket_transfer_histories", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_ticket_transfer_histories_t_maintenance_tickets_maintenance_ticket_id",
                        column: x => x.maintenance_ticket_id,
                        principalTable: "t_maintenance_tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_ticket_transfer_histories_maintenance_ticket_id",
                table: "t_ticket_transfer_histories",
                column: "maintenance_ticket_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_ticket_transfer_histories");
        }
    }
}
