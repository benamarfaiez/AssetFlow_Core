using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.UseCases.Team.GetTeam;
using AssetFlowCore.Domain.Entities;
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
    public async Task ExecuteAsync_WhenTeamExists_ShouldReturnDto()
    {
        var team = new DomainTeam("Infrastructure-Serveurs", "Server", "High", "Équipe serveurs");
        _teamRepoMock
            .Setup(r => r.GetByIdAsync(team.Id))
            .ReturnsAsync(team);

        var query = new GetTeamQuery(team.Id);
        var result = await _handler.ExecuteAsync(query);

        result.Should().NotBeNull();
        result.Should().BeOfType<TeamResponseDto>();
        result.Id.Should().Be(team.Id);
        result.Name.Should().Be("Infrastructure-Serveurs");
        result.Description.Should().Be("Équipe serveurs");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenTeamExists_ShouldMapAllPropertiesCorrectly()
    {
        var team = new DomainTeam("Support-VIP", "Laptop", "Low");
        _teamRepoMock
            .Setup(r => r.GetByIdAsync(team.Id))
            .ReturnsAsync(team);

        var result = await _handler.ExecuteAsync(new GetTeamQuery(team.Id));

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
    public async Task ExecuteAsync_WhenTeamDoesNotExist_ShouldThrowDomainException()
    {
        var unknownId = Guid.NewGuid();
        _teamRepoMock
            .Setup(r => r.GetByIdAsync(unknownId))
            .ReturnsAsync((DomainTeam?)null);

        Func<Task> act = async () => await _handler.ExecuteAsync(new GetTeamQuery(unknownId));

        await act.Should().ThrowAsync<DomainException>()
                 .WithMessage($"*{unknownId}*");
    }

    [Fact]
    public async Task ExecuteAsync_WhenTeamDoesNotExist_ShouldNotReturnDefaultDto()
    {
        var unknownId = Guid.NewGuid();
        _teamRepoMock
            .Setup(r => r.GetByIdAsync(unknownId))
            .ReturnsAsync((DomainTeam?)null);

        Func<Task> act = async () => await _handler.ExecuteAsync(new GetTeamQuery(unknownId));

        await act.Should().ThrowAsync<DomainException>();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Repository interaction
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ShouldCallRepositoryWithCorrectId()
    {
        var team = new DomainTeam("Réseau", "Network", "Medium");
        _teamRepoMock
            .Setup(r => r.GetByIdAsync(team.Id))
            .ReturnsAsync(team);

        await _handler.ExecuteAsync(new GetTeamQuery(team.Id));

        _teamRepoMock.Verify(r => r.GetByIdAsync(team.Id), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCallRepositoryExactlyOnce()
    {
        var team = new DomainTeam("Équipe-BDD", "Database", "Critical");
        _teamRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(team);

        await _handler.ExecuteAsync(new GetTeamQuery(team.Id));

        _teamRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Once);
    }
}
