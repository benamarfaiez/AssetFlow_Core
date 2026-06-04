using AssetFlowCore.Infrastructure.Persistence.Repositories;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
    }
}
