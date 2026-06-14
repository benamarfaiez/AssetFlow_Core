using AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.ValueObjects;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Moq;

namespace AssetFlowCore.UnitTests.Application.UseCases.Assets;

public class DecommissionAssetHandlerTests
{
    private readonly Mock<IAssetRepository> _assetRepoMock = new();
    private readonly Mock<IMaintenanceTicketRepository> _ticketRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DecommissionAssetHandler _handler;

    public DecommissionAssetHandlerTests()
    {
        // On configure l'Unit of Work pour qu'il retourne ce mock de repository
        _uowMock.Setup(u => u.Asset).Returns(_assetRepoMock.Object);
        _uowMock.Setup(u => u.MaintenanceTicket).Returns(_ticketRepoMock.Object);

        _handler = new DecommissionAssetHandler(_uowMock.Object);
    } 

    [Fact]
    public async Task ExecuteAsync_WithNoActiveTickets_ShouldSucceed()
    {
        var asset = new Asset(Guid.NewGuid(), "PC", SerialNumber.Create("SRV12345"), AssetType.Laptop);
        _assetRepoMock.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);
        _ticketRepoMock.Setup(t => t.CountActiveTicketsByAssetIdAsync(asset.Id)).ReturnsAsync(0);

        await _handler.ExecuteAsync(new DecommissionAssetCommand(asset.Id));

        asset.Status.Should().Be(AssetStatus.Decommissioned);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveTickets_ShouldThrowDomainException()
    {
        var asset = new Asset(Guid.NewGuid(), "PC", SerialNumber.Create("SRV12345"), AssetType.Laptop);
        _assetRepoMock.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);
        _ticketRepoMock.Setup(t => t.CountActiveTicketsByAssetIdAsync(asset.Id)).ReturnsAsync(2);

        Func<Task> act = async () => await _handler.ExecuteAsync(new DecommissionAssetCommand(asset.Id));

        await act.Should().ThrowAsync<DomainException>();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAssetNotFound_ShouldThrowDomainException()
    {
        _assetRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Asset?)null);

        Func<Task> act = async () => await _handler.ExecuteAsync(new DecommissionAssetCommand(Guid.NewGuid()));

        await act.Should().ThrowAsync<DomainException>();
    }
}