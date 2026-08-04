using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetFlowCore.Infrastructure.Migrations
{
    /// <summary>
    /// Amorce les équipes de référence couvrant les 9 combinaisons (type d'actif × criticité).
    /// Sans ces lignes, <c>AssignmentStrategyBase.GetTeamNameAsync</c> ne résout aucune équipe
    /// et toute ouverture d'incident échoue sur une base fraîche.
    /// Les noms sont distincts deux à deux : la colonne <c>name</c> porte un index unique.
    /// </summary>
    public partial class SeedReferenceTeams : Migration
    {
        private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] Columns =
            ["id", "name", "description", "is_active", "created_at", "asset_type", "ticket_criticality"];

        private static readonly (string Id, string Name, string Description, string AssetType, string Criticality)[] ReferenceTeams =
        [
            ("7e1c0001-0000-4000-8000-000000000001", "Infrastructure-Serveurs-Critique", "Astreinte serveurs — incidents critiques", "Server", "High"),
            ("7e1c0001-0000-4000-8000-000000000002", "Infrastructure-Serveurs-Standard", "Exploitation serveurs — incidents courants", "Server", "Medium"),
            ("7e1c0001-0000-4000-8000-000000000003", "Infrastructure-Serveurs-Planifie", "Exploitation serveurs — interventions planifiées", "Server", "Low"),
            ("7e1c0001-0000-4000-8000-000000000004", "Support-VIP", "Support de proximité — utilisateurs prioritaires", "Laptop", "High"),
            ("7e1c0001-0000-4000-8000-000000000005", "Support-Bureautique", "Support de proximité — incidents courants", "Laptop", "Medium"),
            ("7e1c0001-0000-4000-8000-000000000006", "Support-Bureautique-Planifie", "Support de proximité — interventions planifiées", "Laptop", "Low"),
            ("7e1c0001-0000-4000-8000-000000000007", "Reseau-Telecom-Critique", "Réseau et télécoms — incidents critiques", "NetworkDevice", "High"),
            ("7e1c0001-0000-4000-8000-000000000008", "Reseau-Telecom-Standard", "Réseau et télécoms — incidents courants", "NetworkDevice", "Medium"),
            ("7e1c0001-0000-4000-8000-000000000009", "Reseau-Telecom-Planifie", "Réseau et télécoms — interventions planifiées", "NetworkDevice", "Low")
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var team in ReferenceTeams)
            {
                migrationBuilder.InsertData(
                    table: "t_teams",
                    columns: Columns,
                    values:
                    [
                        new Guid(team.Id),
                        team.Name,
                        team.Description,
                        true,
                        SeedDate,
                        team.AssetType,
                        team.Criticality
                    ]);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var team in ReferenceTeams)
            {
                migrationBuilder.DeleteData(
                    table: "t_teams",
                    keyColumn: "id",
                    keyValue: new Guid(team.Id));
            }
        }
    }
}
