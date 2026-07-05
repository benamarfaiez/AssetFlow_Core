using AssetFlowCore.Application.UseCases.Assets.GetAllAssets;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace AssetFlowCore.UnitTests.Application.UseCases.Assets;

public class GetAllAssetsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnAllAssetsMappedAsDtos()
    {
        // Arrange
        var repoMock = new Mock<IAssetRepository>();
        var list = new List<Asset> { new(Guid.NewGuid(), "Name", SerialNumber.Create("SERIALX"), AssetType.Server) };
        repoMock.Setup(r => r.GetAllReadOnlyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var handler = new GetAllAssetsHandler(repoMock.Object);
        var query = new GetAllAssetsQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Should().AllBeOfType<AssetFlowCore.Application.DTOs.AssetResponseDto>();
    }
}