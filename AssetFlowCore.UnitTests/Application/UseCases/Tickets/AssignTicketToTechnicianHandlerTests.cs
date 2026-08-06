using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Application.UseCases.Tickets.AssignTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace AssetFlowCore.UnitTests.Application.UseCases.Tickets;

public class AssignTicketToTechnicianHandlerTests
{
    private readonly Mock<IMaintenanceTicketRepository> _ticketRepoMock = new();
    private readonly Mock<IAssetRepository> _assetRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly AssignTicketToTechnicianHandler _handler;

    public AssignTicketToTechnicianHandlerTests() => _handler = new AssignTicketToTechnicianHandler(_ticketRepoMock.Object, _assetRepoMock.Object, _currentUserServiceMock.Object, _uowMock.Object);

    [Fact]
    public async Task ExecuteAsync_WithValidTicket_ShouldAssignAndMutateAsset()
    {
        var asset = new Asset(Guid.NewGuid(), "Laptop", SerialNumber.Create("SRV112"), AssetType.Laptop);
        asset.MarkAsDown();
        var ticket = new MaintenanceTicket(Guid.NewGuid(), asset.Id, "Title", "Desc", TicketCriticality.Low, Guid.NewGuid());
        var assignedByUserId = Guid.NewGuid();

        _ticketRepoMock.Setup(t => t.GetByIdAsync(ticket.Id, CancellationToken.None)).ReturnsAsync(ticket);
        _assetRepoMock.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);
        _currentUserServiceMock.Setup(s => s.GetOrCreateUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(assignedByUserId);

        await _handler.Handle(new AssignTicketToTechnicianCommand(ticket.Id), CancellationToken.None);

        ticket.Status.Should().Be(TicketStatus.InProgress);
        ticket.AssignedByUserId.Should().Be(assignedByUserId);
        asset.Status.Should().Be(AssetStatus.InMaintenance);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingEntities_ShouldThrowDomainException()
    {
        _ticketRepoMock.Setup(t => t.GetByIdAsync(It.IsAny<Guid>(), CancellationToken.None)).ReturnsAsync((MaintenanceTicket?)null);

        Func<Task> act = async () => await _handler.Handle(new AssignTicketToTechnicianCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}