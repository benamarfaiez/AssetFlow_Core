using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Application.Interfaces.RAG;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.ValueObjects;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using DomainTeam = AssetFlowCore.Domain.Entities.Team;

namespace AssetFlowCore.UnitTests.Application.UseCases.Tickets;

public class CreateMaintenanceTicketHandlerTests
{
    private readonly Mock<IAssetRepository> _assetRepoMock = new();
    private readonly Mock<IMaintenanceTicketRepository> _ticketRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ITicketAssignmentEngine> _engineMock = new();
    private readonly Mock<INotificationService> _notifierMock = new();
    private readonly Mock<ITeamRepository> _teamMock = new();
    private readonly Mock<IAIAssistanceQueue> _aiQueue = new();
    private readonly CreateMaintenanceTicketHandler _handler;
     
    private readonly ValidationResult validationResult = new();
    private readonly Mock<IValidator<CreateMaintenanceTicketCommand>> _validator = new();

    public CreateMaintenanceTicketHandlerTests() => _handler = new CreateMaintenanceTicketHandler(_assetRepoMock.Object, _ticketRepoMock.Object, _uowMock.Object, _engineMock.Object, _notifierMock.Object, _teamMock.Object, _aiQueue.Object);

    [Fact]
    public async Task HandleAsync_WithValidAsset_ShouldCreateTicketAndNotify()
    {
        var asset = new Asset(Guid.NewGuid(), "Server", SerialNumber.Create("SRV123"), AssetType.Server);
        _assetRepoMock
            .Setup(r => r.GetByIdAsync(asset.Id))
            .ReturnsAsync(asset);
        _engineMock
            .Setup(e => e.ResolveTeamIdAsync(It.IsAny<AssetType>(), It.IsAny<TicketCriticality>()))
            .ReturnsAsync("Team-Alpha");
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateMaintenanceTicketCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        var team = new DomainTeam("Team-Alpha", "Server", "High", "Description");

        _teamMock
            .Setup(r => r.GetByNameAsync("Team-Alpha"))
            .ReturnsAsync(team);
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateMaintenanceTicketCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var command = new CreateMaintenanceTicketCommand(asset.Id, "Panne", "Détail", "High");
        var result = await _handler.HandleAsync(command);

        result.Should().NotBeNull();
        asset.Status.Should().Be(AssetStatus.Down);
        _ticketRepoMock.Verify(t => t.AddAsync(It.IsAny<MaintenanceTicket>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenAssetDecommissioned_ShouldThrowDomainException()
    {
        var asset = new Asset(Guid.NewGuid(), "Server", SerialNumber.Create("SRV123"), AssetType.Server);
        asset.Decommission();
        _assetRepoMock
            .Setup(r => r.GetByIdAsync(asset.Id))
            .ReturnsAsync(asset);
        _validator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateMaintenanceTicketCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);
        var command = new CreateMaintenanceTicketCommand(asset.Id, "Panne", "Détail", "High");
        Func<Task> act = async () => await _handler.HandleAsync(command);

        await act.Should().ThrowAsync<DomainException>();
    }
}