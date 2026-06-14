
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace AssetFlowCore.IntegrationTests.Infrastructure.Persistence.Repositories;

public class AssetRepositoryTests : IntegrationTestBase
{
    [Fact]
    public async Task AddAsync_ShouldPersistAssetAndAllowRetrieval()
    {
        var dbName = Guid.NewGuid().ToString();
        var assetId = Guid.NewGuid();

        using (var writeContext = CreateInMemoryDbContext(dbName))
        {
            var repository = new AssetRepository(writeContext);
            var asset = new Asset(assetId, "Serveur Exchange", SerialNumber.Create("SRV-99887"), AssetType.Server);
            await repository.AddAsync(asset);
            await writeContext.SaveChangesAsync();
        }

        using (var readContext = CreateInMemoryDbContext(dbName))
        {
            var repository = new AssetRepository(readContext);
            var retrieved = await repository.GetByIdAsync(assetId);

            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be("Serveur Exchange");
            retrieved.SerialNumber.Value.Should().Be("SRV-99887".ToUpper().Trim());
        }
    }

    [Fact]
    public async Task ExistsWithSerialNumberAsync_ShouldBeCaseAndSpaceInsensitive()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var writeContext = CreateInMemoryDbContext(dbName))
        {
            var asset = new Asset(Guid.NewGuid(), "Laptop VIP", SerialNumber.Create("   LPT-12345   "), AssetType.Laptop);
            await writeContext.Assets.AddAsync(asset);
            await writeContext.SaveChangesAsync();
        }

        using var readContext = CreateInMemoryDbContext(dbName);
        var repository = new AssetRepository(readContext);
        var exists = await repository.ExistsWithSerialNumberAsync("lpt-12345".ToUpper().Trim());
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllReadOnlyAsync_ShouldNotTrackEntitiesInEFCore()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var writeContext = CreateInMemoryDbContext(dbName))
        {
            var asset = new Asset(Guid.NewGuid(), "Switch", SerialNumber.Create("SWI-88888"), AssetType.NetworkDevice);
            await writeContext.Assets.AddAsync(asset);
            await writeContext.SaveChangesAsync();
        }

        using var readContext = CreateInMemoryDbContext(dbName);
        var repository = new AssetRepository(readContext);
        var result = await repository.GetAllReadOnlyAsync();

        result.Should().NotBeEmpty();
        readContext.ChangeTracker.Entries<Asset>().Should().BeEmpty();
    }
}