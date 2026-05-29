using Xunit;
using Moq;
using FluentAssertions;
using AssetFlowCore.Application.UseCases.Tickets.AssignTicket;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.ValueObjects;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using System;
using System.Threading.Tasks;

namespace AssetFlowCore.UnitTests.Application.UseCases.Tickets;

public class AssignTicketToTechnicianHandlerTests
{
    private readonly Mock<IMaintenanceTicketRepository> _ticketRepoMock = new();
    private readonly Mock<IAssetRepository> _assetRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly AssignTicketToTechnicianHandler _handler;

    public AssignTicketToTechnicianHandlerTests() => _handler = new AssignTicketToTechnicianHandler(_ticketRepoMock.Object, _assetRepoMock.Object, _uowMock.Object);

    [Fact]
    public async Task ExecuteAsync_WithValidTicket_ShouldAssignAndMutateAsset()
    {
        var asset = new Asset(Guid.NewGuid(), "Laptop", SerialNumber.Create("SRV112"), AssetType.Laptop);
        asset.MarkAsDown();
        var ticket = new MaintenanceTicket(Guid.NewGuid(), asset.Id, "Title", "Desc", TicketCriticality.Low, "Team");

        _ticketRepoMock.Setup(t => t.GetByIdAsync(ticket.Id)).ReturnsAsync(ticket);
        _assetRepoMock.Setup(r => r.GetByIdAsync(asset.Id)).ReturnsAsync(asset);

        await _handler.ExecuteAsync(new AssignTicketToTechnicianCommand(ticket.Id));

        ticket.Status.Should().Be(TicketStatus.InProgress);
        asset.Status.Should().Be(AssetStatus.InMaintenance);
        _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingEntities_ShouldThrowDomainException()
    {
        _ticketRepoMock.Setup(t => t.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((MaintenanceTicket?)null);

        Func<Task> act = async () => await _handler.ExecuteAsync(new AssignTicketToTechnicianCommand(Guid.NewGuid()));

        await act.Should().ThrowAsync<DomainException>();
    }
}