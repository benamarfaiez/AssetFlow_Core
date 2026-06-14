using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Infrastructure.Cache;
using FluentAssertions;
using Moq;

namespace AssetFlowCore.UnitTests.Infrastructure
{
    public class CachedTeamRepositoryTests
    {
        [Fact]
        public async Task GetByIdAsync_ShouldReturnCachedValue_OnSecondCall()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var team = new Team("Team A", "Servers", "High");
            // ensure id matches what the inner repo will return
            // inner repo mock will return the team instance directly

            var innerMock = new Mock<ITeamRepository>();
            innerMock.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);

            var memory = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            var cache = new CachedTeamRepository(innerMock.Object, memory);

            // Act
            var first = await cache.GetByIdAsync(teamId);
            var second = await cache.GetByIdAsync(teamId);

            // Assert
            first.Should().NotBeNull();
            second.Should().NotBeNull();
            second.Should().BeSameAs(first);
            innerMock.Verify(r => r.GetByIdAsync(teamId), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldRefreshCache_WhenTeamUpdated()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var team = new Team("Team A", "Servers", "High", "Description A");

            var innerMock = new Mock<ITeamRepository>();
            innerMock.Setup(r => r.GetByIdAsync(teamId)).ReturnsAsync(team);
            innerMock.Setup(r => r.UpdateAsync(It.IsAny<Team>())).Returns(Task.CompletedTask);

            var memory = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            var cache = new CachedTeamRepository(innerMock.Object, memory);

            // Act - load into cache
            var first = await cache.GetByIdAsync(teamId);

            // Mutate and update
            team.Update("Team A - Renamed", null, null, null);
            await cache.UpdateAsync(team);

            var afterUpdate = await cache.GetByIdAsync(teamId);

            // Assert
            first.Should().NotBeNull();
            afterUpdate.Should().NotBeNull();
            afterUpdate!.Name.Should().Be("Team A - Renamed");

            innerMock.Verify(r => r.GetByIdAsync(teamId), Times.Once);
            innerMock.Verify(r => r.UpdateAsync(It.Is<Team>(t => t.Id == team.Id && t.Name == "Team A - Renamed")), Times.Once);
        }

        [Fact]
        public async Task GetAllActiveAsync_ShouldInvalidateList_AfterAddAsync()
        {
            // Arrange
            var team1 = new Team("Team A", "Servers", "High", "Description A");
            var team2 = new Team("Team B", "Network", "Low", "Description B");

            var innerMock = new Mock<ITeamRepository>();
            innerMock.SetupSequence(r => r.GetAllActiveAsync())
                .ReturnsAsync([team1])
                .ReturnsAsync([team1, team2]);
            innerMock.Setup(r => r.AddAsync(team2)).Returns(Task.CompletedTask);

            var memory = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            var cache = new CachedTeamRepository(innerMock.Object, memory);

            // Act - initial load
            var allActiveFirstList = await cache.GetAllActiveAsync();
            var firstList = allActiveFirstList?.ToList();

            // Act - should be served from cache (no additional inner call)
            var allActiveSecondList = await cache.GetAllActiveAsync();
            var secondList = allActiveSecondList?.ToList();


            // Now add and expect cache invalidation
            await cache.AddAsync(team2);
            var allActiveAfterAdd = await cache.GetAllActiveAsync();
            var afterAdd = allActiveAfterAdd?.ToList();

            // Assert
            firstList.Should().ContainSingle().Which.Should().Be(team1);
            secondList.Should().HaveCount(1);
            afterAdd.Should().HaveCount(2);

            innerMock.Verify(r => r.GetAllActiveAsync(), Times.Exactly(2));
            innerMock.Verify(r => r.AddAsync(team2), Times.Once);
        }

        [Fact]
        public async Task GetAllActiveAsync_ShouldInvalidateList_AfterRemoveAsync()
        {
            // Arrange
            var team1 = new Team("Team A", "Servers", "High", "Description A");
            var team2 = new Team("Team B", "Network", "Low", "Description B");

            var innerMock = new Mock<ITeamRepository>();
            innerMock.SetupSequence(r => r.GetAllActiveAsync())
                .ReturnsAsync([team1, team2])
                .ReturnsAsync([team2]);
            innerMock.Setup(r => r.RemoveAsync(team1)).Returns(Task.CompletedTask);

            var memory = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            var cache = new CachedTeamRepository(innerMock.Object, memory);

            // Act - initial load
            var initialAllActive = await cache.GetAllActiveAsync();
            var initial = initialAllActive?.ToList();
            // Remove and expect invalidation
            await cache.RemoveAsync(team1);
            var afterRemoveAllActive = await cache.GetAllActiveAsync();
            var afterRemove = afterRemoveAllActive?.ToList();

            // Assert
            initial.Should().HaveCount(2);
            afterRemove.Should().HaveCount(1);
            afterRemove.Should().OnlyContain(t => t.Id == team2.Id);

            innerMock.Verify(r => r.GetAllActiveAsync(), Times.Exactly(2));
            innerMock.Verify(r => r.RemoveAsync(team1), Times.Once);
        }
    }
}
