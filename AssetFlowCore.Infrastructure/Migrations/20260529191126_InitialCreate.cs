using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlowCore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    serial_num = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_assets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "t_maintenance_tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    criticality = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    assigned_team = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    resolution_comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_maintenance_tickets", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_maintenance_tickets_t_assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "t_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_assets_serial_num",
                table: "t_assets",
                column: "serial_num",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_maintenance_tickets_AssetId",
                table: "t_maintenance_tickets",
                column: "AssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_maintenance_tickets");

            migrationBuilder.DropTable(
                name: "t_assets");
        }
    }
}
