using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Infrastructure.Cache;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace AssetFlowCore.UnitTests.Infrastructure
{
    public class CachedTeamRepositoryFullCoverageTests
    {
        [Fact]
        public void Ctor_ShouldThrow_OnNullArgs()
        {
            var memory = new MemoryCache(new MemoryCacheOptions());
            var repoMock = new Mock<ITeamRepository>();

            Action a1 = () => _ = new CachedTeamRepository(null!, memory);
            Action a2 = () => _ = new CachedTeamRepository(repoMock.Object, null!);

            a1.Should().Throw<ArgumentNullException>();
            a2.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task GetAllActiveAsync_ShouldCacheResults()
        {
            var team = new Team("CacheAll-Team", "Srv", "High");
            var innerMock = new Mock<ITeamRepository>();
            innerMock.Setup(r => r.GetAllActiveAsync()).ReturnsAsync([team]);

            var memory = new MemoryCache(new MemoryCacheOptions());
            var cache = new CachedTeamRepository(innerMock.Object, memory);

            var firstAllActive = await cache.GetAllActiveAsync();
            var first = firstAllActive?.ToList();
            var secondAllActive = await cache.GetAllActiveAsync();
            var second = secondAllActive?.ToList();

            first.Should().ContainSingle().Which.Should().Be(team);
            second.Should().ContainSingle().Which.Should().Be(team);
            innerMock.Verify(r => r.GetAllActiveAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByNameAsync_ShouldUseCachedList_WhenPresent()
        {
            var team = new Team("ByName-Team", "Net", "Low");
            var innerMock = new Mock<ITeamRepository>();
            innerMock.Setup(r => r.GetAllActiveAsync()).ReturnsAsync([team]);
            // if GetByNameAsync were called, return null to ensure cache path is used
            innerMock.Setup(r => r.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((Team?)null);

            var memory = new MemoryCache(new MemoryCacheOptions());
            var cache = new CachedTeamRepository(innerMock.Object, memory);

            // populate list cache
            var listAllActive = await cache.GetAllActiveAsync();
            var list = listAllActive?.ToList();
            list.Should().ContainSingle();

            var byName = await cache.GetByNameAsync("ByName-Team");
            byName.Should().NotBeNull();
            byName!.Id.Should().Be(team.Id);

            innerMock.Verify(r => r.GetAllActiveAsync(), Times.Once);
            innerMock.Verify(r => r.GetByNameAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetByAssetTypeAndCriticalityAsync_ShouldUseCachedList_WhenPresent()
        {
            var team = new Team("ByType-Team", "TypeX", "Critical");
            var innerMock = new Mock<ITeamRepository>();
            innerMock.Setup(r => r.GetAllActiveAsync()).ReturnsAsync([team]);
            innerMock.Setup(r => r.GetByAssetTypeAndCriticalityAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((Team?)null);

            var memory = new MemoryCache(new MemoryCacheOptions());
            var cache = new CachedTeamRepository(innerMock.Object, memory);

            await cache.GetAllActiveAsync(); // populate
            var found = await cache.GetByAssetTypeAndCriticalityAsync("TypeX", "Critical");
            found.Should().NotBeNull();
            found!.Id.Should().Be(team.Id);

            innerMock.Verify(r => r.GetAllActiveAsync(), Times.Once);
            innerMock.Verify(r => r.GetByAssetTypeAndCriticalityAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExistsWithNameAsync_ShouldReturnTrue_WhenCached()
        {
            var team = new Team("ExistsCache-Team", "T", "C");
            var innerMock = new Mock<ITeamRepository>();
            innerMock.Setup(r => r.GetAllActiveAsync()).ReturnsAsync([team]);
            innerMock.Setup(r => r.ExistsWithNameAsync(It.IsAny<string>())).ReturnsAsync(false);

            var memory = new MemoryCache(new MemoryCacheOptions());
            var cache = new CachedTeamRepository(innerMock.Object, memory);

            await cache.GetAllActiveAsync();
            var exists = await cache.ExistsWithNameAsync("ExistsCache-Team");
            exists.Should().BeTrue();

            innerMock.Verify(r => r.GetAllActiveAsync(), Times.Once);
            innerMock.Verify(r => r.ExistsWithNameAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RefreshCacheFor_ShouldPopulateIdEntry_AndAffectGetById()
        {
            var team = new Team("Refresh-Team", "T", "C");
            var innerMock = new Mock<ITeamRepository>();
            innerMock.Setup(r => r.GetByIdAsync(team.Id, CancellationToken.None)).ReturnsAsync((Team?)null);

            var memory = new MemoryCache(new MemoryCacheOptions());
            var cache = new CachedTeamRepository(innerMock.Object, memory);

            // initially inner would return null
            var before = await cache.GetByIdAsync(team.Id);
            before.Should().BeNull();

            // refresh cache manually
            cache.RefreshCacheFor(team);

            var after = await cache.GetByIdAsync(team.Id);
            after.Should().NotBeNull();
            after!.Id.Should().Be(team.Id);

            // since cache had the value, inner GetByIdAsync should not be called for the cached read
            innerMock.Verify(r => r.GetByIdAsync(team.Id, CancellationToken.None), Times.Once); // initial call only
        }
    }
}
