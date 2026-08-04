using AssetFlowCore.Application.UseCases.Team.GetTeams;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using Moq;
using DomainTeam = AssetFlowCore.Domain.Entities.Team;

namespace AssetFlowCore.UnitTests.Application.UseCases.Team;

public class GetTeamsHandlerTests
{
    private readonly Mock<ITeamRepository> _teamRepository = new();
    private readonly GetTeamsHandler _handler;

    private readonly DomainTeam _active = new("Équipe active", "Server", "High", "Astreinte");
    private readonly DomainTeam _desactivee;

    public GetTeamsHandlerTests()
    {
        _desactivee = new DomainTeam("Équipe dissoute", "Laptop", "Low", "Support");
        _desactivee.Deactivate();

        _teamRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync([_active, _desactivee]);
        _teamRepository.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync([_active]);

        _handler = new GetTeamsHandler(_teamRepository.Object);
    }

    [Fact]
    public async Task Handle_ByDefault_ShouldReturnEveryTeam()
    {
        var result = await _handler.Handle(new GetTeamsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        _teamRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _teamRepository.Verify(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OnlyActive_ShouldQueryTheActiveTeamsOnly()
    {
        var result = await _handler.Handle(new GetTeamsQuery(OnlyActive: true), CancellationToken.None);

        result.Should().ContainSingle().Which.IsActive.Should().BeTrue();
        _teamRepository.Verify(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _teamRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldExposeTheRoutingCoupleOfEachTeam()
    {
        var result = await _handler.Handle(new GetTeamsQuery(), CancellationToken.None);

        var equipe = result.Should().Contain(t => t.Name == "Équipe active").Subject;
        equipe.AssetType.Should().Be("Server");
        equipe.TicketCriticality.Should().Be("High");
    }
}
