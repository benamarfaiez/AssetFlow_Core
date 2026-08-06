using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlowCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndAuthorTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "assigned_by_user_id",
                table: "t_maintenance_tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "closed_by_user_id",
                table: "t_maintenance_tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "t_users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    external_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    team_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_users_t_teams_team_id",
                        column: x => x.team_id,
                        principalTable: "t_teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_maintenance_tickets_assigned_by_user_id",
                table: "t_maintenance_tickets",
                column: "assigned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_maintenance_tickets_closed_by_user_id",
                table: "t_maintenance_tickets",
                column: "closed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_t_users_external_id",
                table: "t_users",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_users_team_id",
                table: "t_users",
                column: "team_id");

            migrationBuilder.AddForeignKey(
                name: "FK_t_maintenance_tickets_t_users_assigned_by_user_id",
                table: "t_maintenance_tickets",
                column: "assigned_by_user_id",
                principalTable: "t_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_t_maintenance_tickets_t_users_closed_by_user_id",
                table: "t_maintenance_tickets",
                column: "closed_by_user_id",
                principalTable: "t_users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_maintenance_tickets_t_users_assigned_by_user_id",
                table: "t_maintenance_tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_t_maintenance_tickets_t_users_closed_by_user_id",
                table: "t_maintenance_tickets");

            migrationBuilder.DropTable(
                name: "t_users");

            migrationBuilder.DropIndex(
                name: "IX_t_maintenance_tickets_assigned_by_user_id",
                table: "t_maintenance_tickets");

            migrationBuilder.DropIndex(
                name: "IX_t_maintenance_tickets_closed_by_user_id",
                table: "t_maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "assigned_by_user_id",
                table: "t_maintenance_tickets");

            migrationBuilder.DropColumn(
                name: "closed_by_user_id",
                table: "t_maintenance_tickets");
        }
    }
}
