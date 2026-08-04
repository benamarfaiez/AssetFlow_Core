using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Application.UseCases.Assets.DecommissionAsset;
using AssetFlowCore.Application.UseCases.Team.DeleteTeam;
using AssetFlowCore.Application.UseCases.Tickets.CloseTicket;
using AssetFlowCore.Application.UseCases.Tickets.TransferTicket;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Domain.ValueObjects;
using AssetFlowCore.Application.Services;
using FluentAssertions;
using Moq;
using DomainTeam = AssetFlowCore.Domain.Entities.Team;

namespace AssetFlowCore.UnitTests.Application.UseCases;

/// <summary>
/// Couvre la correction 1.8 : le jeton d'annulation reçu par le cas d'usage doit atteindre
/// chaque dépôt. Auparavant, <c>ITeamRepository</c> et <c>IMaintenanceTicketRepository</c>
/// n'exposaient pas de paramètre d'annulation : l'abandon du client par le client HTTP
/// n'interrompait aucune requête de lecture.
/// </summary>
public class CancellationTokenPropagationTests
{
    private static readonly CancellationToken Token = new CancellationTokenSource().Token;

    [Fact]
    public async Task DecommissionAssetHandler_ShouldPassTokenToTicketRepository()
    {
        var asset = new Asset(Guid.NewGuid(), "PC", SerialNumber.Create("TOKEN-1"), AssetType.Laptop);
        var assetRepo = new Mock<IAssetRepository>();
        var ticketRepo = new Mock<IMaintenanceTicketRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Asset).Returns(assetRepo.Object);
        uow.Setup(u => u.MaintenanceTicket).Returns(ticketRepo.Object);
        assetRepo.Setup(r => r.GetByIdAsync(asset.Id, Token)).ReturnsAsync(asset);

        await new DecommissionAssetHandler(uow.Object).Handle(new DecommissionAssetCommand(asset.Id), Token);

        assetRepo.Verify(r => r.GetByIdAsync(asset.Id, Token), Times.Once);
        ticketRepo.Verify(r => r.CountActiveTicketsByAssetIdAsync(asset.Id, Token), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(Token), Times.Once);
    }

    [Fact]
    public async Task CloseTicketHandler_ShouldPassTokenToBothRepositories()
    {
        var asset = new Asset(Guid.NewGuid(), "PC", SerialNumber.Create("TOKEN-2"), AssetType.Laptop);
        asset.MarkAsDown();
        asset.MarkInMaintenance();
        var ticket = new MaintenanceTicket(Guid.NewGuid(), asset.Id, "Titre", "Desc", TicketCriticality.Low, Guid.NewGuid());
        ticket.AssignToTechnician();

        var ticketRepo = new Mock<IMaintenanceTicketRepository>();
        var assetRepo = new Mock<IAssetRepository>();
        var uow = new Mock<IUnitOfWork>();
        ticketRepo.Setup(r => r.GetByIdAsync(ticket.Id, Token)).ReturnsAsync(ticket);
        assetRepo.Setup(r => r.GetByIdAsync(asset.Id, Token)).ReturnsAsync(asset);

        await new CloseTicketHandler(ticketRepo.Object, assetRepo.Object, uow.Object)
            .Handle(new CloseTicketCommand(ticket.Id, "Réparé"), Token);

        ticketRepo.Verify(r => r.HasOtherActiveTicketsAsync(asset.Id, ticket.Id, Token), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(Token), Times.Once);
    }

    [Fact]
    public async Task DeleteTeamCommandHandler_ShouldPassTokenToTeamAndTicketRepositories()
    {
        var team = new DomainTeam("Support", "Laptop", "Low");
        var teamRepo = new Mock<ITeamRepository>();
        var ticketRepo = new Mock<IMaintenanceTicketRepository>();
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.Team).Returns(teamRepo.Object);
        uow.Setup(u => u.MaintenanceTicket).Returns(ticketRepo.Object);
        teamRepo.Setup(r => r.GetByIdAsync(team.Id, Token)).ReturnsAsync(team);
        ticketRepo.Setup(r => r.ExistsActiveTicketsForTeamAsync(team.Id, Token)).ReturnsAsync(false);

        await new DeleteTeamCommandHandler(uow.Object).Handle(new DeleteTeamCommand(team.Id), Token);

        teamRepo.Verify(r => r.RemoveAsync(team, Token), Times.Once);
    }

    [Fact]
    public async Task RequestTicketTransferCommandHandler_ShouldPassTokenToRepositories()
    {
        var team = new DomainTeam("Réseau", "NetworkDevice", "High");
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Desc", TicketCriticality.Low, Guid.NewGuid());

        var ticketRepo = new Mock<IMaintenanceTicketRepository>();
        var teamRepo = new Mock<ITeamRepository>();
        var uow = new Mock<IUnitOfWork>();
        ticketRepo.Setup(r => r.GetByIdWithTrackingAsync(ticket.Id, Token)).ReturnsAsync(ticket);
        teamRepo.Setup(r => r.GetByNameAsync("Réseau", Token)).ReturnsAsync(team);

        await new RequestTicketTransferCommandHandler(ticketRepo.Object, teamRepo.Object, uow.Object)
            .Handle(new RequestTicketTransferCommand(ticket.Id, "Réseau", "Expertise réseau"), Token);

        ticketRepo.Verify(r => r.GetByIdWithTrackingAsync(ticket.Id, Token), Times.Once);
        teamRepo.Verify(r => r.GetByNameAsync("Réseau", Token), Times.Once);
    }

    [Fact]
    public async Task TicketAssignmentEngine_ShouldPassTokenToTeamRepository()
    {
        var team = new DomainTeam("Infrastructure-Serveurs", "Server", "High");
        var teamRepo = new Mock<ITeamRepository>();
        teamRepo.Setup(r => r.GetByAssetTypeAndCriticalityAsync("Server", "High", Token)).ReturnsAsync(team);

        var engine = new TicketAssignmentEngine(
            [new ServerAssignmentStrategy(teamRepo.Object)]);

        var resolved = await engine.ResolveTeamIdAsync(AssetType.Server, TicketCriticality.High, Token);

        resolved.Should().Be("Infrastructure-Serveurs");
        teamRepo.Verify(r => r.GetByAssetTypeAndCriticalityAsync("Server", "High", Token), Times.Once);
    }
}
