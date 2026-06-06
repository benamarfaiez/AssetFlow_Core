using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using System.Collections.Concurrent;
using Xunit;

namespace AssetFlowCore.IntegrationTests.Infrastructure.Persistence.Repositories
{
    public class TeamRepositoryConcurrencyTests : IntegrationTestBase
    {
        [Fact]
        public async Task ConcurrentUpdates_ShouldNotThrowAndPersistOneOfUpdates()
        {
            // Arrange: create initial team in a shared in-memory database
            var dbName = Guid.NewGuid().ToString();
            var team = new Team("Concurrent-Team", "Server", "Medium");
            using (var seedContext = CreateInMemoryDbContext(dbName))
            {
                var seedRepo = new TeamRepository(seedContext);
                await seedRepo.AddAsync(team);
                await seedContext.SaveChangesAsync();
            }

            var exceptions = new ConcurrentQueue<Exception>();
            const int concurrency = 8;

            // Act: run concurrent update operations in parallel, each with its own DbContext
            var tasks = Enumerable.Range(0, concurrency).Select(i => Task.Run(async () =>
            {
                try
                {
                    using var ctx = CreateInMemoryDbContext(dbName);
                    var repo = new TeamRepository(ctx);
                    var t = await repo.GetByIdAsync(team.Id);
                    if (t == null) return;

                    // Each task writes a slightly different name
                    t.Update($"Concurrent-Team-{i}", null, null, null);
                    await repo.UpdateAsync(t);
                    await ctx.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            // Assert: no exceptions and final state persisted (last-writer-wins is acceptable)
            exceptions.IsEmpty.Should().BeTrue();

            using var verifyCtx = CreateInMemoryDbContext(dbName);
            var verifyRepo = new TeamRepository(verifyCtx);
            var final = await verifyRepo.GetByIdAsync(team.Id);
            final.Should().NotBeNull();
            final!.Name.Should().StartWith("Concurrent-Team-");
        }
    }
}
