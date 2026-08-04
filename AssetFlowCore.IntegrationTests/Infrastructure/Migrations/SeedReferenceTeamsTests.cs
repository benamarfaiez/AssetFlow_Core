using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Application.Services;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Infrastructure.Migrations;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace AssetFlowCore.IntegrationTests.Infrastructure.Migrations;

/// <summary>
/// Couvre la correction 1.9 : sur une base fraîche, aucune équipe de référence n'existait et
/// toute ouverture d'incident échouait sur une <c>DomainException</c> émise par le moteur
/// d'assignation. Le test rejoue l'amorçage de la migration, puis vérifie que les 9 combinaisons
/// (type d'actif × criticité) résolvent bien une équipe.
/// </summary>
public class SeedReferenceTeamsTests : IntegrationTestBase
{
    private const int IdColumn = 0;
    private const int NameColumn = 1;
    private const int DescriptionColumn = 2;
    private const int AssetTypeColumn = 5;
    private const int CriticalityColumn = 6;

    private static IReadOnlyList<InsertDataOperation> SeedOperations()
        => [.. new SeedReferenceTeams().UpOperations.OfType<InsertDataOperation>()];

    [Fact]
    public void Migration_ShouldSeedNineTeams_WithDistinctNamesAndIdentifiers()
    {
        var operations = SeedOperations();

        operations.Should().HaveCount(9);
        operations.Should().OnlyContain(o => o.Table == "t_teams");

        // La colonne name porte un index unique : des noms dupliqués feraient échouer la migration.
        operations.Select(o => o.Values[0, NameColumn]).Should().OnlyHaveUniqueItems();
        operations.Select(o => o.Values[0, IdColumn]).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Migration_ShouldCoverEveryAssetTypeAndCriticalityCombination()
    {
        var couples = SeedOperations()
            .Select(o => ($"{o.Values[0, AssetTypeColumn]}", $"{o.Values[0, CriticalityColumn]}"))
            .ToList();

        var attendus =
            from type in Enum.GetNames<AssetType>()
            from criticality in Enum.GetNames<TicketCriticality>()
            select (type, criticality);

        couples.Should().BeEquivalentTo(attendus);
    }

    [Fact]
    public async Task SeededDatabase_ShouldResolveATeam_ForEveryCombination()
    {
        // Arrange : base fraîche amorcée avec les seules données de la migration
        using var context = CreateInMemoryDbContext();
        foreach (var operation in SeedOperations())
        {
            context.Teams.Add(new Team(
                $"{operation.Values[0, NameColumn]}",
                $"{operation.Values[0, AssetTypeColumn]}",
                $"{operation.Values[0, CriticalityColumn]}",
                $"{operation.Values[0, DescriptionColumn]}"));
        }
        await context.SaveChangesAsync();

        var teamRepository = new TeamRepository(context);
        var engine = new TicketAssignmentEngine(
        [
            new ServerAssignmentStrategy(teamRepository),
            new NetworkAssignmentStrategy(teamRepository),
            new LaptopHighCriticalityStrategy(teamRepository),
            new LaptopStandardStrategy(teamRepository)
        ]);

        // Act & Assert : les 9 combinaisons doivent résoudre une équipe existante
        foreach (var assetType in Enum.GetValues<AssetType>())
        {
            foreach (var criticality in Enum.GetValues<TicketCriticality>())
            {
                var teamName = await engine.ResolveTeamIdAsync(assetType, criticality);

                teamName.Should().NotBeNullOrWhiteSpace(
                    $"la combinaison {assetType}/{criticality} doit être couverte par l'amorçage");
                (await teamRepository.GetByNameAsync(teamName)).Should().NotBeNull();
            }
        }
    }
}
