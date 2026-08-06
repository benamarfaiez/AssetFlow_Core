using AssetFlowCore.Application.UseCases.Assets.RestoreAssetToService;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AssetFlowCore.UnitTests.Application.UseCases.Assets;

public class RestoreAssetToServiceHandlerTests
{
    private readonly Mock<IAssetRepository> _assetRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly RestoreAssetToServiceHandler _handler;

    public RestoreAssetToServiceHandlerTests()
    {
        _uowMock.Setup(u => u.Asset).Returns(_assetRepoMock.Object);
        _handler = new RestoreAssetToServiceHandler(_uowMock.Object, Mock.Of<ILogger<RestoreAssetToServiceHandler>>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenDecommissioned_ShouldRestoreToServiceAndPersist()
    {
        var asset = new Asset(Guid.NewGuid(), "PC", SerialNumber.Create("SRV12345"), AssetType.Laptop);
        asset.Decommission();
        _assetRepoMock.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        await _handler.Handle(new RestoreAssetToServiceCommand(asset.Id, "Rebut par erreur"), CancellationToken.None);

        asset.Status.Should().Be(AssetStatus.InService);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAssetNotFound_ShouldThrowNotFoundException()
    {
        _assetRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Asset?)null);

        Func<Task> act = async () => await _handler.Handle(new RestoreAssetToServiceCommand(Guid.NewGuid(), "Motif"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotDecommissioned_ShouldThrowDomainException()
    {
        var asset = new Asset(Guid.NewGuid(), "PC", SerialNumber.Create("SRV12345"), AssetType.Laptop);
        _assetRepoMock.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        Func<Task> act = async () => await _handler.Handle(new RestoreAssetToServiceCommand(asset.Id, "Motif"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_WithBlankReason_ShouldThrowArgumentException(string reason)
    {
        var asset = new Asset(Guid.NewGuid(), "PC", SerialNumber.Create("SRV12345"), AssetType.Laptop);
        asset.Decommission();
        _assetRepoMock.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        Func<Task> act = async () => await _handler.Handle(new RestoreAssetToServiceCommand(asset.Id, reason), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
