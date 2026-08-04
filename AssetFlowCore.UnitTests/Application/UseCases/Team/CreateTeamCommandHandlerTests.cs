using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Team.CreateTeam;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using Moq;
using DomainTeam = AssetFlowCore.Domain.Entities.Team;

namespace AssetFlowCore.UnitTests.Application.UseCases.Team;

public class CreateTeamCommandHandlerTests
{
    private readonly Mock<ITeamRepository> _teamRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateTeamCommandHandler _handler;

    public CreateTeamCommandHandlerTests()
    {
        // On lie les sous-propriétés de l'Unit of Work à nos mocks de repositories
        _uowMock.Setup(u => u.Team).Returns(_teamRepoMock.Object);
        _handler = new CreateTeamCommandHandler(_uowMock.Object);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Happy path
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_ShouldPersistTeamAndReturnDto()
    {
        // Arrange
        var command = new CreateTeamCommand("Infrastructure-Serveurs", "Server", "High", "Équipe serveurs");

        // Act
        // CORRECTION : Appel à .Handle() avec CancellationToken
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<TeamResponseDto>();
        result.Name.Should().Be("Infrastructure-Serveurs");
        result.IsActive.Should().BeTrue();
        _teamRepoMock.Verify(r => r.AddAsync(It.IsAny<DomainTeam>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldSucceedAndReturnDtoWithNullDescription()
    {
        // Arrange
        var command = new CreateTeamCommand("Support-VIP", "Laptop", "Low", null);

        // Act
        // CORRECTION : Appel à .Handle() avec CancellationToken
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Description.Should().BeNull();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnDtoWithNonEmptyId()
    {
        // Arrange
        var command = new CreateTeamCommand("Réseau", "Network", "Medium", null);

        // Act
        // CORRECTION : Appel à .Handle() avec CancellationToken
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnDtoWithCorrectProperties()
    {
        // Arrange
        var command = new CreateTeamCommand("Équipe-BDD", "Database", "Critical", "Base de données de prod");

        // Act
        // CORRECTION : Appel à .Handle() avec CancellationToken
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("Équipe-BDD");
        result.Description.Should().Be("Base de données de prod");
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ShouldThrowDomainExceptionAndNotPersist()
    {
        // Arrange : correction 1.4 — le doublon était laissé à l'index unique de la base
        _teamRepoMock.Setup(r => r.ExistsWithNameAsync("Infrastructure-Serveurs", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var command = new CreateTeamCommand("  Infrastructure-Serveurs  ", "Server", "High", null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
                 .WithMessage("Une équipe nommée 'Infrastructure-Serveurs' existe déjà.");

        _teamRepoMock.Verify(r => r.AddAsync(It.IsAny<DomainTeam>(), It.IsAny<CancellationToken>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Domain validation – entity constructor guards
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("", "Server", "High")]
    [InlineData("   ", "Server", "High")]
    [InlineData(null, "Server", "High")]
    public async Task Handle_WithInvalidName_ShouldThrowArgumentException(
        string? name, string assetType, string criticality)
    {
        // Arrange
        var command = new CreateTeamCommand(name!, assetType, criticality, null);

        // Act
        // CORRECTION : Appel à .Handle() avec CancellationToken dans le délégué
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*Le nom de l'équipe est obligatoire*");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Équipe-X", "", "High")]
    [InlineData("Équipe-X", "   ", "High")]
    [InlineData("Équipe-X", null, "High")]
    public async Task Handle_WithInvalidAssetType_ShouldThrowArgumentException(
        string name, string? assetType, string criticality)
    {
        // Arrange
        var command = new CreateTeamCommand(name, assetType!, criticality, null);

        // Act
        // CORRECTION : Appel à .Handle() avec CancellationToken dans le délégué
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*Le assetType de l'équipe est obligatoire*");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Équipe-X", "Server", "")]
    [InlineData("Équipe-X", "Server", "   ")]
    [InlineData("Équipe-X", "Server", null)]
    public async Task Handle_WithInvalidTicketCriticality_ShouldThrowArgumentException(
        string name, string assetType, string? criticality)
    {
        // Arrange
        var command = new CreateTeamCommand(name, assetType, criticality!, null);

        // Act
        // CORRECTION : Appel à .Handle() avec CancellationToken dans le délégué
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*Le ticketCriticality de l'équipe est obligatoire*");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
