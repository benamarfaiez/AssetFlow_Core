using AssetFlowCore.Application.UseCases.Tickets.TransferTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using DomainTeam = AssetFlowCore.Domain.Entities.Team;

namespace AssetFlowCore.UnitTests.Application.UseCases.Tickets;

public class RequestTicketTransferCommandHandlerTests
{
    private readonly Mock<IMaintenanceTicketRepository> _ticketRepositoryMock;
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RequestTicketTransferCommandHandler _handler;

    public RequestTicketTransferCommandHandlerTests()
    {
        _ticketRepositoryMock = new Mock<IMaintenanceTicketRepository>();
        _teamRepositoryMock = new Mock<ITeamRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new RequestTicketTransferCommandHandler(
            _ticketRepositoryMock.Object,
            _teamRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_Should_PersistChanges_When_TicketExistsAndValid()
    {
        // Arrange
        var team = new DomainTeam("Nouvelle-Equipe", "Server", TicketCriticality.Low.ToString(), "Description");

        _teamRepositoryMock
            .Setup(r => r.GetByNameAsync(team.Name))
            .ReturnsAsync(team);

        var command = new RequestTicketTransferCommand(Guid.NewGuid(), team.Name, "Motif valide");

        var asset = new Asset(Guid.NewGuid(), "PC", SerialNumber.Create("SRV999"), AssetType.Laptop);
        asset.MarkAsDown();
        asset.MarkInMaintenance();

        var existingTicket = new MaintenanceTicket(Guid.NewGuid(), asset.Id, "Title", "Desc", TicketCriticality.Low, Guid.NewGuid());

        _ticketRepositoryMock
            .Setup(repo => repo.GetByIdWithTrackingAsync(command.TicketId))
            .ReturnsAsync(existingTicket);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        // On vérifie que la sauvegarde a bien été appelée exactement 1 fois
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        existingTicket.AssignedTeam.Name.Should().Be(team.Name);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ThrowDomainException_When_TicketNotFound()
    {
        // Arrange
        var command = new RequestTicketTransferCommand(Guid.NewGuid(), "Nouvelle-Equipe", "Motif valide");

        _ticketRepositoryMock
            .Setup(repo => repo.GetByIdWithTrackingAsync(command.TicketId))
            .ReturnsAsync((MaintenanceTicket?)null); // Simule un retour vide de la BDD

        // Act
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<DomainException>()
                    .WithMessage("Ticket introuvable.");

        // On s'assure que SaveChanges n'a JAMAIS été appelé
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}