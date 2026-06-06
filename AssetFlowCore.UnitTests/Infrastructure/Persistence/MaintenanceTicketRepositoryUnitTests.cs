using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AssetFlowCore.UnitTests.Infrastructure.Persistence
{
    public class MaintenanceTicketRepositoryUnitTests
    {
        private static AssetFlowDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AssetFlowDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            return new AssetFlowDbContext(options);
        }

        [Fact]
        public async Task CountActiveTicketsByAssetIdAsync_ShouldOnlyCountOpenedOrInProgress()
        {
            var dbName = Guid.NewGuid().ToString();
            var assetId = Guid.NewGuid();

            using (var ctx = CreateContext(dbName))
            {
                var teamId = Guid.NewGuid();
                var t1 = new MaintenanceTicket(Guid.NewGuid(), assetId, "T1", "d", TicketCriticality.Low, teamId);
                var t2 = new MaintenanceTicket(Guid.NewGuid(), assetId, "T2", "d", TicketCriticality.Low, teamId);
                t2.AssignToTechnician();
                var t3 = new MaintenanceTicket(Guid.NewGuid(), assetId, "T3", "d", TicketCriticality.Low, teamId);
                t3.AssignToTechnician();
                t3.Close("ok");

                await ctx.Tickets.AddRangeAsync(t1, t2, t3);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = CreateContext(dbName))
            {
                var repo = new MaintenanceTicketRepository(ctx);
                var count = await repo.CountActiveTicketsByAssetIdAsync(assetId);
                count.Should().Be(2);
            }
        }

        [Fact]
        public async Task HasOtherActiveTicketsAsync_ShouldExcludeGivenTicket()
        {
            var dbName = Guid.NewGuid().ToString();
            var assetId = Guid.NewGuid();

            using (var ctx = CreateContext(dbName))
            {
                var teamId = Guid.NewGuid();
                var keep = new MaintenanceTicket(Guid.NewGuid(), assetId, "Keep", "d", TicketCriticality.Low, teamId);
                var other = new MaintenanceTicket(Guid.NewGuid(), assetId, "Other", "d", TicketCriticality.Low, teamId);
                await ctx.Tickets.AddRangeAsync(keep, other);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = CreateContext(dbName))
            {
                var repo = new MaintenanceTicketRepository(ctx);
                var all = await ctx.Tickets.ToListAsync();
                var t0 = all.First();
                var exists = await repo.HasOtherActiveTicketsAsync(t0.AssetId, t0.Id);
                exists.Should().BeTrue();
            }
        }

        [Fact]
        public async Task ExistsActiveTicketsForTeamAsync_ShouldReturnTrue_WhenAssignedOpenOrInProgress()
        {
            var dbName = Guid.NewGuid().ToString();
            var assetId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            using (var ctx = CreateContext(dbName))
            {
                var t1 = new MaintenanceTicket(Guid.NewGuid(), assetId, "A", "d", TicketCriticality.Low, teamId);
                var t2 = new MaintenanceTicket(Guid.NewGuid(), assetId, "B", "d", TicketCriticality.Low, teamId);
                t2.AssignToTechnician();
                await ctx.Tickets.AddRangeAsync(t1, t2);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = CreateContext(dbName))
            {
                var repo = new MaintenanceTicketRepository(ctx);
                var exists = await repo.ExistsActiveTicketsForTeamAsync(teamId);
                exists.Should().BeTrue();
            }
        }
    }
}
