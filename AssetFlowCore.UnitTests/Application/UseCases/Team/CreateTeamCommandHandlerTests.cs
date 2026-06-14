using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Team.CreateTeam;
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
    public async Task HandleAsync_WithValidCommand_ShouldPersistTeamAndReturnDto()
    {
        var command = new CreateTeamCommand("Infrastructure-Serveurs", "Server", "High", "Équipe serveurs");

        var result = await _handler.HandleAsync(command);

        result.Should().NotBeNull();
        result.Should().BeOfType<TeamResponseDto>();
        result.Name.Should().Be("Infrastructure-Serveurs");
        result.IsActive.Should().BeTrue();
        _teamRepoMock.Verify(r => r.AddAsync(It.IsAny<DomainTeam>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullDescription_ShouldSucceedAndReturnDtoWithNullDescription()
    {
        var command = new CreateTeamCommand("Support-VIP", "Laptop", "Low", null);

        var result = await _handler.HandleAsync(command);

        result.Description.Should().BeNull();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldReturnDtoWithNonEmptyId()
    {
        var command = new CreateTeamCommand("Réseau", "Network", "Medium", null);

        var result = await _handler.HandleAsync(command);

        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldReturnDtoWithCorrectProperties()
    {
        var command = new CreateTeamCommand("Équipe-BDD", "Database", "Critical", "Base de données de prod");

        var result = await _handler.HandleAsync(command);

        result.Name.Should().Be("Équipe-BDD");
        result.Description.Should().Be("Base de données de prod");
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Domain validation – entity constructor guards
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("", "Server", "High")]
    [InlineData("   ", "Server", "High")]
    [InlineData(null, "Server", "High")]
    public async Task HandleAsync_WithInvalidName_ShouldThrowArgumentException(
        string? name, string assetType, string criticality)
    {
        var command = new CreateTeamCommand(name!, assetType, criticality, null);

        Func<Task> act = async () => await _handler.HandleAsync(command);

        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*Le nom de l'équipe est obligatoire*");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Équipe-X", "", "High")]
    [InlineData("Équipe-X", "   ", "High")]
    [InlineData("Équipe-X", null, "High")]
    public async Task HandleAsync_WithInvalidAssetType_ShouldThrowArgumentException(
        string name, string? assetType, string criticality)
    {
        var command = new CreateTeamCommand(name, assetType!, criticality, null);

        Func<Task> act = async () => await _handler.HandleAsync(command);

        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*Le assetType de l'équipe est obligatoire*");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Équipe-X", "Server", "")]
    [InlineData("Équipe-X", "Server", "   ")]
    [InlineData("Équipe-X", "Server", null)]
    public async Task HandleAsync_WithInvalidTicketCriticality_ShouldThrowArgumentException(
        string name, string assetType, string? criticality)
    {
        var command = new CreateTeamCommand(name, assetType, criticality!, null);

        Func<Task> act = async () => await _handler.HandleAsync(command);

        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*Le ticketCriticality de l'équipe est obligatoire*");
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
