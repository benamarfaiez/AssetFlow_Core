using AssetFlowCore.Application.UseCases.Tickets.GetTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using Moq;
using DomainTeam = AssetFlowCore.Domain.Entities.Team;

namespace AssetFlowCore.UnitTests.Application.UseCases.Tickets;

public class GetTicketHandlerTests
{
    private readonly Mock<IMaintenanceTicketRepository> _ticketRepoMock = new();
    private readonly Mock<ITeamRepository> _teamRepoMock = new();
    private readonly GetTicketHandler _handler;

    public GetTicketHandlerTests() => _handler = new GetTicketHandler(_ticketRepoMock.Object, _teamRepoMock.Object);

    [Fact]
    public async Task ExecuteAsync_WhenTicketExists_ShouldReturnCorrectTicketResponse()
    {
        var team = new DomainTeam("Team-Alpha", "Server", TicketCriticality.Low.ToString(), "Description");

        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Title", "Desc", TicketCriticality.Low, team.Id);

        ticket.AssignToTechnician();

        _ticketRepoMock.Setup(t => t.GetByIdAsync(ticket.Id, CancellationToken.None)).ReturnsAsync(ticket);
        _ticketRepoMock.Setup(t => t.CountActiveTicketsByAssetIdAsync(Guid.NewGuid(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _teamRepoMock
            .Setup(r => r.GetByIdAsync(team.Id, CancellationToken.None))
            .ReturnsAsync(team);

        var result = await _handler.Handle(new GetTicketQuery(ticket.Id), CancellationToken.None);

        result.Status.Should().Be(ticket.Status.ToString());
        result.Title.Should().Be(ticket.Title);
        result.Criticality.Should().Be(ticket.Criticality.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_WhenTicketDoesNotExist_ShouldThrowDomainException()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var query = new GetTicketQuery(ticketId);

        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Title", "Desc", TicketCriticality.Low, Guid.NewGuid());
        _ticketRepoMock.Setup(t => t.GetByIdAsync(ticket.Id, CancellationToken.None)).ReturnsAsync(ticket);
        _ticketRepoMock.Setup(t => t.CountActiveTicketsByAssetIdAsync(Guid.NewGuid(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage($"Le ticket avec l'ID {ticketId} est introuvable.");
    }
}
