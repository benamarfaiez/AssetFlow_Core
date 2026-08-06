using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace AssetFlowCore.IntegrationTests.Infrastructure.Persistence.Repositories;

public class MaintenanceTicketRepositoryTests : IntegrationTestBase
{
    [Fact]
    public async Task CountActiveTicketsByAssetIdAsync_ShouldOnlyCountOpenedOrInProgressTickets()
    {
        var dbName = Guid.NewGuid().ToString();
        var assetId = Guid.NewGuid();

        using (var writeContext = CreateInMemoryDbContext(dbName))
        {
            var teamId = Guid.NewGuid();
            var t1 = new MaintenanceTicket(Guid.NewGuid(), assetId, "Ticket 1", "Desc", TicketCriticality.Low, teamId);
            var t2 = new MaintenanceTicket(Guid.NewGuid(), assetId, "Ticket 2", "Desc", TicketCriticality.Low, teamId);
            t2.AssignToTechnician(Guid.NewGuid());

            var t3 = new MaintenanceTicket(Guid.NewGuid(), assetId, "Ticket 3", "Desc", TicketCriticality.Low, teamId);
            t3.AssignToTechnician(Guid.NewGuid());
            t3.Close(Guid.NewGuid(), "Resolved");

            await writeContext.Tickets.AddRangeAsync(t1, t2, t3);
            await writeContext.SaveChangesAsync();
        }

        using var readContext = CreateInMemoryDbContext(dbName);
        var repository = new MaintenanceTicketRepository(readContext);
        var activeCount = await repository.CountActiveTicketsByAssetIdAsync(assetId);

        activeCount.Should().Be(2);
    }

    [Fact]
    public async Task TransferToTeam_Then_AddTransferHistoryAsync_ShouldPersist_WithoutConcurrencyConflict()
    {
        // Réaffecter l'équipe (RowVersion) et ajouter une entrée d'historique doivent pouvoir
        // se persister dans le même SaveChanges — la navigation TransferHistory est ignorée par
        // EF précisément pour permettre cette combinaison (voir MaintenanceTicket.LoadTransferHistory).
        var dbName = Guid.NewGuid().ToString();
        var ticketId = Guid.NewGuid();

        Team teamOld, teamNew;
        using (var writeContext = CreateInMemoryDbContext(dbName))
        {
            teamOld = new Team("Equipe-Origine", "Laptop", "Low", "desc");
            teamNew = new Team("Equipe-Cible", "Laptop", "Low", "desc");
            var ticket = new MaintenanceTicket(ticketId, Guid.NewGuid(), "Titre", "Description", TicketCriticality.Low, teamOld.Id);
            await writeContext.Teams.AddRangeAsync(teamOld, teamNew);
            await writeContext.Tickets.AddAsync(ticket);
            await writeContext.SaveChangesAsync();
        }

        using var context = CreateInMemoryDbContext(dbName);
        var repository = new MaintenanceTicketRepository(context);
        var ticketLoaded = await repository.GetByIdWithTrackingAsync(ticketId);
        var teamNewLoaded = await context.Teams.FindAsync(teamNew.Id);

        var historyEntry = ticketLoaded!.TransferToTeam(teamNewLoaded!, "Besoin d'une expertise réseau.");
        await repository.AddTransferHistoryAsync(historyEntry);
        await context.SaveChangesAsync();

        var history = await repository.GetTransferHistoryAsync(ticketId);
        history.Should().ContainSingle();
        history.Single().FromTeamId.Should().Be(teamOld.Id);
        history.Single().ToTeamId.Should().Be(teamNew.Id);
    }
}
