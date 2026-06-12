using AssetFlowCore.Application.UseCases.Team.DeleteTeam;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace AssetFlowCore.UnitTests.Application.UseCases.Team;

public class DeleteTeamCommandHandlerTests
{
    private readonly Mock<ITeamRepository> _teamRepo = new();
    private readonly Mock<IMaintenanceTicketRepository> _ticketRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly DeleteTeamCommandHandler _handler;

    public DeleteTeamCommandHandlerTests()
        => _handler = new DeleteTeamCommandHandler(_uow.Object);

    [Fact]
    public async Task ExecuteAsync_WithNoAssignedActiveTickets_ShouldRemoveTeam()
    {
        var team = new AssetFlowCore.Domain.Entities.Team("Equipe-Test", "Server", "High", null);
        _teamRepo.Setup(r => r.GetByIdAsync(team.Id)).ReturnsAsync(team);
        _ticketRepo.Setup(r => r.ExistsActiveTicketsForTeamAsync(team.Id)).ReturnsAsync(false);

        await _handler.ExecuteAsync(new DeleteTeamCommand(team.Id));

        _teamRepo.Verify(r => r.RemoveAsync(team), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveTickets_ShouldThrowDomainException()
    {
        var team = new AssetFlowCore.Domain.Entities.Team("Equipe-Test", "Server", "High", null);
        _teamRepo.Setup(r => r.GetByIdAsync(team.Id)).ReturnsAsync(team);
        _ticketRepo.Setup(r => r.ExistsActiveTicketsForTeamAsync(team.Id)).ReturnsAsync(true);

        Func<Task> act = async () => await _handler.ExecuteAsync(new DeleteTeamCommand(team.Id));

        await act.Should().ThrowAsync<AssetFlowCore.Domain.Exceptions.DomainException>();
        _teamRepo.Verify(r => r.RemoveAsync(It.IsAny<AssetFlowCore.Domain.Entities.Team>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
