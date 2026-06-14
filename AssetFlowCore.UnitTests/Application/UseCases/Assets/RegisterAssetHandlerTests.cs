using AssetFlowCore.Application.UseCases.Assets.RegisterAsset;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace AssetFlowCore.UnitTests.Application.UseCases.Assets;

public class RegisterAssetHandlerTests
{
    private readonly Mock<IAssetRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly RegisterAssetHandler _handler;

    public RegisterAssetHandlerTests()
    {
        // On configure l'Unit of Work pour qu'il retourne ce mock de repository
        _uowMock.Setup(u => u.Asset).Returns(_repoMock.Object);
        _handler = new RegisterAssetHandler(_uowMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenNewAsset_ShouldSaveAndReturnDto()
    {
        _repoMock.Setup(r => r.ExistsWithSerialNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var command = new RegisterAssetCommand("Asset-01", "SERIAL123", "Server");

        var result = await _handler.HandleAsync(command);

        result.Should().NotBeNull();
        result.Name.Should().Be("Asset-01");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenSerialAlreadyExists_ShouldThrowDomainException()
    {
        _repoMock.Setup(r => r.ExistsWithSerialNumberAsync("SERIAL123".ToUpper().Trim(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var command = new RegisterAssetCommand("Asset-01", "SERIAL123", "Server");

        Func<Task> act = async () => await _handler.HandleAsync(command);

        await act.Should().ThrowAsync<DomainException>();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}