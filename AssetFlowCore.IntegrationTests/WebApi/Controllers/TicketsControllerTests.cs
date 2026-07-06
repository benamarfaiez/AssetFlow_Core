using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.WebApi.Requests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi.Controllers;

public class TicketsControllerTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task CreateTicket_WithValidAsset_ShouldMutateAssetAndReturnCreated()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        var teamName = "Team A";
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            await context.Database.EnsureDeletedAsync();
            var asset = new Asset(assetId, "Laptop Intégration", SerialNumber.Create("LPT-INT-95"), AssetType.Laptop);
            var team = new Team(teamName, AssetType.Laptop.ToString(), TicketCriticality.Medium.ToString(), "Description de la Team A");

            await context.Assets.AddAsync(asset);
            await context.Teams.AddAsync(team);
            await context.SaveChangesAsync();
        }

        var payload = new CreateTicketRequest(
            assetId,
            "Correction dysfonctionnement clavier matériel",
            "Laptop présente plusieurs touches complètement hors service suite à un incident.",
            "Medium"
        );

        var response = await _client.PostAsJsonAsync("/api/tickets", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var ticket = await response.Content.ReadFromJsonAsync<TicketResponseDto>();
        Assert.NotNull(ticket);
        Assert.Equal(teamName, ticket.AssignedTeamName);
    }

    [Fact]
    public async Task TransferTicket_Should_ReturnNoContent_And_UpdateDatabase()
    {
        // Arrange
        var client = _factory.CreateClient();
        var ticketId = Guid.NewGuid();
        var teamName = "Infra-Réseaux";
        var requestPayload = new TransferTicketRequest(teamName, "Problème de switch");
        var teamNew = new Team(teamName, AssetType.Laptop.ToString(), TicketCriticality.Medium.ToString(), "Description de la Team");

        // --- Préparation de la base de données ---
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            await dbContext.Database.EnsureDeletedAsync();

            var teamOld = new Team("Equipe A", AssetType.Laptop.ToString(), TicketCriticality.Medium.ToString(), "Description de la Team A");

            var ticket = new MaintenanceTicket(ticketId, Guid.NewGuid(), "titre", "description", TicketCriticality.Low, teamOld.Id);

            await dbContext.Tickets.AddAsync(ticket);
            await dbContext.Teams.AddAsync(teamNew);
            await dbContext.Teams.AddAsync(teamOld);
            await dbContext.SaveChangesAsync();
        }

        // Act : Appel du endpoint API réel
        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/transfer", requestPayload);

        // Assert : Vérification de la réponse HTTP
        response.StatusCode.Should().Be(HttpStatusCode.NoContent); // 204

        // Assert : Vérification que la base de données a VRAIMENT changé
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            var updatedTicket = await dbContext.Tickets.FindAsync(ticketId);

            updatedTicket.Should().NotBeNull();
            updatedTicket.AssignedTeamId.Should().Be(teamNew.Id);
        }
    }

    [Fact]
    public async Task GetTicket_WithValidId_ShouldReturn200OkAndPayload()
    {
        var client = _factory.CreateClient();
        var ticketId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();

            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();

            // 1. Créer et insérer un Asset valide
            var asset = new Asset(Guid.NewGuid(), "PC Portable Test", SerialNumber.Create("SN-TEST123"), AssetType.Laptop);
            dbContext.Assets.Add(asset);
            await dbContext.SaveChangesAsync();

            // 2. Créer la Team
            var team = new Team("teamName", AssetType.Laptop.ToString(), TicketCriticality.Medium.ToString(), "Description de la Team A");
            dbContext.Teams.Add(team);
            await dbContext.SaveChangesAsync();

            // 3. Créer le Ticket en lui passant le VRAI id de l'asset
            var ticket = new MaintenanceTicket(ticketId, asset.Id, "titre", "description", TicketCriticality.Low, team.Id);
            dbContext.Tickets.Add(ticket);
            await dbContext.SaveChangesAsync();
        }

        // Act
        var response = await client.GetAsync($"/api/tickets/{ticketId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var assets = await response.Content.ReadFromJsonAsync<TicketResponseDto>();
        assets.Should().NotBeNull();
        assets.Id.Should().Be(ticketId);
    }

    [Fact]
    public async Task AssignTicket_WithValidData_ShouldReturnNoContent_And_UpdateStatuses()
    {
        var ticketId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            await db.Database.EnsureDeletedAsync();

            var asset = new Asset(Guid.NewGuid(), "Srv Broken", SerialNumber.Create("ASSIGN-1"), AssetType.Server);
            asset.MarkAsDown();
            await db.Assets.AddAsync(asset);

            var team = new Team("Ops", AssetType.Server.ToString(), TicketCriticality.Low.ToString(), "Ops");
            await db.Teams.AddAsync(team);

            var ticket = new MaintenanceTicket(ticketId, asset.Id, "titre", "desc", TicketCriticality.Low, team.Id);
            await db.Tickets.AddAsync(ticket);
            await db.SaveChangesAsync();
        }

        var resp = await _client.PutAsync($"/api/tickets/{ticketId}/assign", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            var ticket = await db.Tickets.FindAsync(ticketId);
            ticket.Should().NotBeNull();
            ticket!.Status.Should().Be(TicketStatus.InProgress);

            var asset = await db.Assets.FindAsync(ticket.AssetId);
            asset.Should().NotBeNull();
            asset!.Status.Should().Be(Domain.Enums.AssetStatus.InMaintenance);
        }
    }

    [Fact]
    public async Task AssignTicket_NotFound_ShouldReturnBadRequest()
    {
        var resp = await _client.PutAsync($"/api/tickets/{Guid.NewGuid()}/assign", null);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CloseTicket_WithValidData_ShouldReturnNoContent_And_RestoreAssetWhenNoOtherActiveTickets()
    {
        var ticketId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            await db.Database.EnsureDeletedAsync();

            var asset = new Asset(Guid.NewGuid(), "Srv To Close", SerialNumber.Create("CLOSE-1"), AssetType.Server);
            asset.MarkAsDown();
            await db.Assets.AddAsync(asset);

            var team = new Team("OpsClose", AssetType.Server.ToString(), TicketCriticality.Low.ToString(), "Ops");
            await db.Teams.AddAsync(team);

            var ticket = new MaintenanceTicket(ticketId, asset.Id, "titre", "desc", TicketCriticality.Low, team.Id);
            ticket.AssignToTechnician();
            await db.Tickets.AddAsync(ticket);
            await db.SaveChangesAsync();
        }

        var payload = new CloseTicketRequest("Resolution ok");
        var resp = await _client.PutAsJsonAsync($"/api/tickets/{ticketId}/close", payload);
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            var ticket = await db.Tickets.FindAsync(ticketId);
            ticket.Should().NotBeNull();
            ticket!.Status.Should().Be(TicketStatus.Closed);
            ticket.ResolutionComment.Should().Be("Resolution ok");

            var asset = await db.Assets.FindAsync(ticket.AssetId);
            asset.Should().NotBeNull();
            asset!.Status.Should().Be(Domain.Enums.AssetStatus.InService);
        }
    }

    [Fact]
    public async Task CloseTicket_NotFound_ShouldReturnBadRequest()
    {
        var payload = new CloseTicketRequest("x");
        var resp = await _client.PutAsJsonAsync($"/api/tickets/{Guid.NewGuid()}/close", payload);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TransferTicket_TargetTeamNotFound_ShouldReturnBadRequest()
    {
        var ticketId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            await db.Database.EnsureDeletedAsync();
            var team = new Team("Old", AssetType.Server.ToString(), TicketCriticality.Low.ToString(), "Old");
            var ticket = new MaintenanceTicket(ticketId, Guid.NewGuid(), "titre", "desc", TicketCriticality.Low, team.Id);
            await db.Tickets.AddAsync(ticket);
            await db.Teams.AddAsync(team);
            await db.SaveChangesAsync();
        }

        var payload = new TransferTicketRequest("NonExistingTeam", "reason");
        var resp = await _client.PostAsJsonAsync($"/api/tickets/{ticketId}/transfer", payload);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTicket_WithDecommissionedAsset_ShouldReturnBadRequest()
    {
        var assetId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            await db.Database.EnsureDeletedAsync();
            var asset = new Asset(assetId, "Decom", SerialNumber.Create("DEC-1"), AssetType.Server);
            asset.Decommission();
            await db.Assets.AddAsync(asset);
            await db.SaveChangesAsync();
        }

        var payload = new CreateTicketRequest(assetId, "t", "d", "Low");
        var resp = await _client.PostAsJsonAsync("/api/tickets", payload);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}