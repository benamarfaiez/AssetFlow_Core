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
    public async Task Handle_WhenNewAsset_ShouldSaveAndReturnDto()
    {
        // Arrange
        _repoMock.Setup(r => r.ExistsWithSerialNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var command = new RegisterAssetCommand("Asset-01", "SERIAL123", "Server");

        // Act
        // CORRECTION : Appel à .Handle() avec CancellationToken
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Asset-01");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSerialAlreadyExists_ShouldThrowDomainException()
    {
        // Arrange
        _repoMock.Setup(r => r.ExistsWithSerialNumberAsync("SERIAL123".ToUpper().Trim(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var command = new RegisterAssetCommand("Asset-01", "SERIAL123", "Server");

        // Act
        // CORRECTION : Appel à .Handle() avec CancellationToken dans le délégué
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Ce numéro de série constructeur est déjà enregistré dans le parc.");

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}