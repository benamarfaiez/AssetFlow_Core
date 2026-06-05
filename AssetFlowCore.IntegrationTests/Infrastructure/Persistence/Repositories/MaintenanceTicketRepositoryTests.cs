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
            t2.AssignToTechnician();

            var t3 = new MaintenanceTicket(Guid.NewGuid(), assetId, "Ticket 3", "Desc", TicketCriticality.Low, teamId);
            t3.AssignToTechnician();
            t3.Close("Resolved");

            await writeContext.Tickets.AddRangeAsync(t1, t2, t3);
            await writeContext.SaveChangesAsync();
        }

        using var readContext = CreateInMemoryDbContext(dbName);
        var repository = new MaintenanceTicketRepository(readContext);
        var activeCount = await repository.CountActiveTicketsByAssetIdAsync(assetId);

        activeCount.Should().Be(2);
    }
}