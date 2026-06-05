using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlowCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_maintenance_tickets_t_assets_AssetId",
                table: "t_maintenance_tickets");

            migrationBuilder.DropIndex(
                name: "IX_t_maintenance_tickets_AssetId",
                table: "t_maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "assigned_team",
                table: "t_maintenance_tickets");

            migrationBuilder.RenameColumn(
                name: "AssetId",
                table: "t_maintenance_tickets",
                newName: "asset_id");

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_team_id",
                table: "t_maintenance_tickets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "t_teams",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    asset_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ticket_criticality = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_teams", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_maintenance_tickets_asset_id_status",
                table: "t_maintenance_tickets",
                columns: new[] { "asset_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_t_tickets_assigned_team_id",
                table: "t_maintenance_tickets",
                column: "assigned_team_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_teams_is_active",
                table: "t_teams",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_t_teams_name",
                table: "t_teams",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_t_maintenance_tickets_t_assets_asset_id",
                table: "t_maintenance_tickets",
                column: "asset_id",
                principalTable: "t_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_t_maintenance_tickets_t_teams_assigned_team_id",
                table: "t_maintenance_tickets",
                column: "assigned_team_id",
                principalTable: "t_teams",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_maintenance_tickets_t_assets_asset_id",
                table: "t_maintenance_tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_t_maintenance_tickets_t_teams_assigned_team_id",
                table: "t_maintenance_tickets");

            migrationBuilder.DropTable(
                name: "t_teams");

            migrationBuilder.DropIndex(
                name: "IX_t_maintenance_tickets_asset_id_status",
                table: "t_maintenance_tickets");

            migrationBuilder.DropIndex(
                name: "IX_t_tickets_assigned_team_id",
                table: "t_maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "assigned_team_id",
                table: "t_maintenance_tickets");

            migrationBuilder.RenameColumn(
                name: "asset_id",
                table: "t_maintenance_tickets",
                newName: "AssetId");

            migrationBuilder.AddColumn<string>(
                name: "assigned_team",
                table: "t_maintenance_tickets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_t_maintenance_tickets_AssetId",
                table: "t_maintenance_tickets",
                column: "AssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_t_maintenance_tickets_t_assets_AssetId",
                table: "t_maintenance_tickets",
                column: "AssetId",
                principalTable: "t_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
