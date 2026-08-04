using AssetFlowCore.Application.UseCases.Assets.GetAsset;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace AssetFlowCore.UnitTests.Application.UseCases.Assets;

public class GetAssetHandlerTests
{
    private readonly Mock<IAssetRepository> _assetRepository = new();
    private readonly GetAssetHandler _handler;

    public GetAssetHandlerTests() => _handler = new GetAssetHandler(_assetRepository.Object);

    [Fact]
    public async Task Handle_ShouldReturnTheAssetWithoutItsTickets_WhenItHasNone()
    {
        var asset = new Asset(Guid.NewGuid(), "Commutateur", SerialNumber.Create("NET-01"), AssetType.NetworkDevice);
        _assetRepository.Setup(r => r.GetByIdWithTicketsAsync(asset.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(asset);

        var result = await _handler.Handle(new GetAssetQuery(asset.Id), CancellationToken.None);

        result.Id.Should().Be(asset.Id);
        result.SerialNumber.Should().Be("NET-01");
        result.Type.Should().Be("NetworkDevice");
        result.Tickets.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenAssetDoesNotExist_ShouldThrowNotFoundException()
    {
        var unknownId = Guid.NewGuid();
        _assetRepository.Setup(r => r.GetByIdWithTicketsAsync(unknownId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Asset?)null);

        Func<Task> act = async () => await _handler.Handle(new GetAssetQuery(unknownId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
                 .WithMessage($"L'actif {unknownId} est introuvable.");
    }

    [Fact]
    public async Task Handle_ShouldPassTheCancellationTokenToTheRepository()
    {
        var token = new CancellationTokenSource().Token;
        var asset = new Asset(Guid.NewGuid(), "Serveur", SerialNumber.Create("SRV-01"), AssetType.Server);
        _assetRepository.Setup(r => r.GetByIdWithTicketsAsync(asset.Id, token)).ReturnsAsync(asset);

        await _handler.Handle(new GetAssetQuery(asset.Id), token);

        _assetRepository.Verify(r => r.GetByIdWithTicketsAsync(asset.Id, token), Times.Once);
    }
}
