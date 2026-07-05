using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Team.GetTeam;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using Moq;
using DomainTeam = AssetFlowCore.Domain.Entities.Team;

namespace AssetFlowCore.UnitTests.Application.UseCases.Team;

public class GetTeamHandlerTests
{
    private readonly Mock<ITeamRepository> _teamRepoMock = new();
    private readonly GetTeamHandler _handler;

    public GetTeamHandlerTests()
        => _handler = new GetTeamHandler(_teamRepoMock.Object);

    // ──────────────────────────────────────────────────────────────────────
    // Happy path
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTeamExists_ShouldReturnDto()
    {
        // Arrange
        var team = new DomainTeam("Infrastructure-Serveurs", "Server", "High", "Équipe serveurs");
        _teamRepoMock
            .Setup(r => r.GetByIdAsync(team.Id, CancellationToken.None))
            .ReturnsAsync(team);

        var query = new GetTeamQuery(team.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<TeamResponseDto>();
        result.Id.Should().Be(team.Id);
        result.Name.Should().Be("Infrastructure-Serveurs");
        result.Description.Should().Be("Équipe serveurs");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTeamExists_ShouldMapAllPropertiesCorrectly()
    {
        // Arrange
        var team = new DomainTeam("Support-VIP", "Laptop", "Low");
        _teamRepoMock
            .Setup(r => r.GetByIdAsync(team.Id, CancellationToken.None))
            .ReturnsAsync(team);

        // Act
        var result = await _handler.Handle(new GetTeamQuery(team.Id), CancellationToken.None);

        // Assert
        result.Id.Should().Be(team.Id);
        result.Name.Should().Be(team.Name);
        result.Description.Should().BeNull();
        result.IsActive.Should().Be(team.IsActive);
        result.CreatedAt.Should().Be(team.CreatedAt);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Not found
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTeamDoesNotExist_ShouldThrowDomainException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _teamRepoMock
            .Setup(r => r.GetByIdAsync(unknownId, CancellationToken.None))
            .ReturnsAsync((DomainTeam?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(new GetTeamQuery(unknownId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>().WithMessage($"Le team avec l'ID {unknownId} est introuvable.");
    }

    [Fact]
    public async Task Handle_WhenTeamDoesNotExist_ShouldNotReturnDefaultDto()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _teamRepoMock
            .Setup(r => r.GetByIdAsync(unknownId, CancellationToken.None))
            .ReturnsAsync((DomainTeam?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(new GetTeamQuery(unknownId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Repository interaction
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldCallRepositoryWithCorrectId()
    {
        // Arrange
        var team = new DomainTeam("Réseau", "Network", "Medium");
        _teamRepoMock
            .Setup(r => r.GetByIdAsync(team.Id, CancellationToken.None))
            .ReturnsAsync(team);

        // Act
        await _handler.Handle(new GetTeamQuery(team.Id), CancellationToken.None);

        // Assert
        _teamRepoMock.Verify(r => r.GetByIdAsync(team.Id, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryExactlyOnce()
    {
        // Arrange
        var team = new DomainTeam("Équipe-BDD", "Database", "Critical");
        _teamRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None))
            .ReturnsAsync(team);

        // Act
        await _handler.Handle(new GetTeamQuery(team.Id), CancellationToken.None);

        // Assert
        _teamRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None), Times.Once);
    }
}
