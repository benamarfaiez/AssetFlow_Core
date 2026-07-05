using AssetFlowCore.Application.UseCases.Team.DeleteTeam;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using Moq;
using DomainTeam = AssetFlowCore.Domain.Entities.Team;

namespace AssetFlowCore.UnitTests.Application.UseCases.Team;

public class DeleteTeamCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ITeamRepository> _teamRepoMock = new();
    private readonly Mock<IMaintenanceTicketRepository> _ticketRepoMock = new();
    private readonly DeleteTeamCommandHandler _handler;

    public DeleteTeamCommandHandlerTests()
    {
        // Setup de l'Unit of Work pour retourner nos mocks de dépôts
        _uowMock.Setup(u => u.Team).Returns(_teamRepoMock.Object);
        _uowMock.Setup(u => u.MaintenanceTicket).Returns(_ticketRepoMock.Object);

        _handler = new DeleteTeamCommandHandler(_uowMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTeamExistsAndHasNoActiveTickets_ShouldDeleteTeamAndSaveChanges()
    {
        // Arrange
        var team = new DomainTeam("Support", "Laptop", "Low");
        _teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, CancellationToken.None)).ReturnsAsync(team);
        _ticketRepoMock.Setup(r => r.ExistsActiveTicketsForTeamAsync(team.Id)).ReturnsAsync(false);

        var command = new DeleteTeamCommand(team.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _teamRepoMock.Verify(r => r.RemoveAsync(team), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTeamNotFound_ShouldThrowDomainException()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _teamRepoMock.Setup(r => r.GetByIdAsync(unknownId, CancellationToken.None)).ReturnsAsync((DomainTeam?)null);

        var command = new DeleteTeamCommand(unknownId);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Team introuvable.");

        _teamRepoMock.Verify(r => r.RemoveAsync(It.IsAny<DomainTeam>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTeamHasActiveTickets_ShouldThrowDomainExceptionAndNotDelete()
    {
        // Arrange
        var team = new DomainTeam("Réseau", "Network", "High");
        _teamRepoMock.Setup(r => r.GetByIdAsync(team.Id, CancellationToken.None)).ReturnsAsync(team);
        _ticketRepoMock.Setup(r => r.ExistsActiveTicketsForTeamAsync(team.Id)).ReturnsAsync(true);

        var command = new DeleteTeamCommand(team.Id);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Impossible de supprimer le team : des tickets actifs lui sont assignes.");

        _teamRepoMock.Verify(r => r.RemoveAsync(It.IsAny<DomainTeam>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}