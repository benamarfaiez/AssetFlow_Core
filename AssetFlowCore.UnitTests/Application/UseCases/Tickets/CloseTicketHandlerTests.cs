using AssetFlowCore.Application.UseCases.Tickets.CloseTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace AssetFlowCore.UnitTests.Application.UseCases.Tickets;

public class CloseTicketHandlerTests
{
    private readonly Mock<IMaintenanceTicketRepository> _ticketRepoMock = new();
    private readonly Mock<IAssetRepository> _assetRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CloseTicketHandler _handler;

    public CloseTicketHandlerTests() => _handler = new CloseTicketHandler(_ticketRepoMock.Object, _assetRepoMock.Object, _uowMock.Object);

    [Fact]
    public async Task ExecuteAsync_WhenLastActiveTicket_ShouldRestoreAssetToService()
    {
        var asset = new Asset(Guid.NewGuid(), "PC", SerialNumber.Create("SRV999"), AssetType.Laptop);
        asset.MarkAsDown();
        asset.MarkInMaintenance();

        var ticket = new MaintenanceTicket(Guid.NewGuid(), asset.Id, "Title", "Desc", TicketCriticality.Low, Guid.NewGuid());
        ticket.AssignToTechnician();

        _ticketRepoMock.Setup(t => t.GetByIdAsync(ticket.Id)).ReturnsAsync(ticket);
        _assetRepoMock.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);
        _ticketRepoMock.Setup(t => t.CountActiveTicketsByAssetIdAsync(asset.Id)).ReturnsAsync(1); // Uniquement celui-ci

        await _handler.ExecuteAsync(new CloseTicketCommand(ticket.Id, "Repaired"));

        ticket.Status.Should().Be(TicketStatus.Closed);
        asset.Status.Should().Be(AssetStatus.InService);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}