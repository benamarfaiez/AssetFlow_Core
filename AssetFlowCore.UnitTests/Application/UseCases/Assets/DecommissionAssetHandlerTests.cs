using Xunit;
using Moq;
using FluentAssertions;
using AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.ValueObjects;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using System;
using System.Threading.Tasks;

namespace AssetFlowCore.UnitTests.Application.UseCases.Assets;

public class DecommissionAssetHandlerTests
{
    private readonly Mock<IAssetRepository> _assetRepoMock = new();
    private readonly Mock<IMaintenanceTicketRepository> _ticketRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DecommissionAssetHandler _handler;

    public DecommissionAssetHandlerTests() => _handler = new DecommissionAssetHandler(_assetRepoMock.Object, _ticketRepoMock.Object, _uowMock.Object);

    [Fact]
    public async Task ExecuteAsync_WithNoActiveTickets_ShouldSucceed()
    {
        var asset = new Asset(Guid.NewGuid(), "PC", SerialNumber.Create("SRV12345"), AssetType.Laptop);
        _assetRepoMock.Setup(r => r.GetByIdAsync(asset.Id)).ReturnsAsync(asset);
        _ticketRepoMock.Setup(t => t.CountActiveTicketsByAssetIdAsync(asset.Id)).ReturnsAsync(0);

        await _handler.ExecuteAsync(new DecommissionAssetCommand(asset.Id));

        asset.Status.Should().Be(AssetStatus.Decommissioned);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveTickets_ShouldThrowDomainException()
    {
        var asset = new Asset(Guid.NewGuid(), "PC", SerialNumber.Create("SRV12345"), AssetType.Laptop);
        _assetRepoMock.Setup(r => r.GetByIdAsync(asset.Id)).ReturnsAsync(asset);
        _ticketRepoMock.Setup(t => t.CountActiveTicketsByAssetIdAsync(asset.Id)).ReturnsAsync(2);

        Func<Task> act = async () => await _handler.ExecuteAsync(new DecommissionAssetCommand(asset.Id));

        await act.Should().ThrowAsync<DomainException>();
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAssetNotFound_ShouldThrowDomainException()
    {
        _assetRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Asset?)null);

        Func<Task> act = async () => await _handler.ExecuteAsync(new DecommissionAssetCommand(Guid.NewGuid()));

        await act.Should().ThrowAsync<DomainException>();
    }
}