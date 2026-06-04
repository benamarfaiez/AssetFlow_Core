using Xunit;
using Moq;
using FluentAssertions;
using AssetFlowCore.Application.UseCases.Assets.GetAllAssets;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.ValueObjects;
using AssetFlowCore.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AssetFlowCore.UnitTests.Application.UseCases.Assets;

public class GetAllAssetsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnAllAssetsMappedAsDtos()
    {
        var repoMock = new Mock<IAssetRepository>();
        var list = new List<Asset> { new(Guid.NewGuid(), "Name", SerialNumber.Create("SERIALX"), AssetType.Server) };
        repoMock.Setup(r => r.GetAllReadOnlyAsync()).ReturnsAsync(list);

        var handler = new GetAllAssetsHandler(repoMock.Object);
        var result = await handler.HandleAsync(new GetAllAssetsQuery());

        result.Should().HaveCount(1);
    }
}