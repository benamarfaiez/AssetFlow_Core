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
        var teamName= "Team A";
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
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

            var teamOld= new Team("Equipe A", AssetType.Laptop.ToString(), TicketCriticality.Medium.ToString(), "Description de la Team A");

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
            var team = new Team("teamName", AssetType.Laptop.ToString(), TicketCriticality.Medium.ToString(), "Description de la Team A");
            dbContext.Teams.Add(team);
            dbContext.Tickets.Add(new MaintenanceTicket(ticketId, Guid.NewGuid(), "titre", "description", TicketCriticality.Low, team.Id));
            await dbContext.SaveChangesAsync();
        }
        var response = await client.GetAsync($"/api/tickets/{ticketId}");

        var assets = await response.Content.ReadFromJsonAsync<TicketResponseDto>();
        assets.Should().NotBeNull();
        assets.Id.Should().Be(ticketId);
    }
}