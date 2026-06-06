using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace AssetFlowCore.IntegrationTests.Infrastructure.Persistence.Repositories
{
    public class TeamRepositoryTests : IntegrationTestBase
    {
        [Fact]
        public async Task AddAndGetById_ShouldPersistTeam()
        {
            using var context = CreateInMemoryDbContext();
            var repo = new TeamRepository(context);

            var team = new Team("Repo-Team", "Laptop", "Medium", "desc");
            await repo.AddAsync(team);
            await context.SaveChangesAsync();

            var fetched = await repo.GetByIdAsync(team.Id);
            fetched.Should().NotBeNull();
            fetched!.Name.Should().Be("Repo-Team");
        }

        [Fact]
        public async Task ExistsWithName_ShouldReturnTrueWhenPresent()
        {
            using var context = CreateInMemoryDbContext();
            var repo = new TeamRepository(context);

            var team = new Team("Exists-Team", "Server", "Low", null);
            await repo.AddAsync(team);
            await context.SaveChangesAsync();

            var exists = await repo.ExistsWithNameAsync("Exists-Team");
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateAsync_ShouldPersistChanges()
        {
            using var context = CreateInMemoryDbContext();
            var repo = new TeamRepository(context);

            var team = new Team("ToUpdate-Team", "Server", "High");
            await repo.AddAsync(team);
            await context.SaveChangesAsync();

            // Modify and update
            team.Update("ToUpdate-Team-Renamed", null, null, null);
            await repo.UpdateAsync(team);
            await context.SaveChangesAsync();

            var fetched = await repo.GetByIdAsync(team.Id);
            fetched.Should().NotBeNull();
            fetched!.Name.Should().Be("ToUpdate-Team-Renamed");
        }

        [Fact]
        public async Task RemoveAsync_ShouldDeleteTeam()
        {
            using var context = CreateInMemoryDbContext();
            var repo = new TeamRepository(context);

            var team = new Team("ToRemove-Team", "Laptop", "Low");
            await repo.AddAsync(team);
            await context.SaveChangesAsync();

            await repo.RemoveAsync(team);
            await context.SaveChangesAsync();

            var fetched = await repo.GetByIdAsync(team.Id);
            fetched.Should().BeNull();
        }

        [Fact]
        public async Task GetAllActiveAsync_ShouldReturnOnlyActiveTeams()
        {
            using var context = CreateInMemoryDbContext();
            var repo = new TeamRepository(context);

            var active = new Team("Active-Team", "Network", "Medium");
            var inactive = new Team("Inactive-Team", "Network", "Medium");
            inactive.Deactivate();

            await repo.AddAsync(active);
            await repo.AddAsync(inactive);
            await context.SaveChangesAsync();

            var list = (await repo.GetAllActiveAsync()).ToList();
            list.Should().ContainSingle().Which.Id.Should().Be(active.Id);
        }
    }
}
