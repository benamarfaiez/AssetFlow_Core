using AssetFlowCore.Application.UseCases.Tickets.GetTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace AssetFlowCore.UnitTests.Application.UseCases.Tickets;

public class GetTicketHandlerTests
{
    private readonly Mock<IMaintenanceTicketRepository> _ticketRepoMock = new();
    private readonly GetTicketHandler _handler;

    public GetTicketHandlerTests() => _handler = new GetTicketHandler(_ticketRepoMock.Object);

    [Fact]
    public async Task ExecuteAsync_WhenTicketExists_ShouldReturnCorrectTicketResponse()
    {
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Title", "Desc", TicketCriticality.Low, "Team");
        ticket.AssignToTechnician();

        _ticketRepoMock.Setup(t => t.GetByIdAsync(ticket.Id)).ReturnsAsync(ticket);
        _ticketRepoMock.Setup(t => t.CountActiveTicketsByAssetIdAsync(Guid.NewGuid())).ReturnsAsync(1); // Uniquement celui-ci

        var result = await _handler.ExecuteAsync(new GetTicketQuery(ticket.Id));

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

        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Title", "Desc", TicketCriticality.Low, "Team");
        _ticketRepoMock.Setup(t => t.GetByIdAsync(ticket.Id)).ReturnsAsync(ticket);
        _ticketRepoMock.Setup(t => t.CountActiveTicketsByAssetIdAsync(Guid.NewGuid())).ReturnsAsync(1);

        // Act
        Func<Task> act = async () => await _handler.ExecuteAsync(query);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage($"Le ticket avec l'ID {ticketId} est introuvable.");
    }
}