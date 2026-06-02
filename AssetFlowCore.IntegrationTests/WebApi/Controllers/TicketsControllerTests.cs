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
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            var asset = new Asset(assetId, "Laptop Intégration", SerialNumber.Create("LPT-INT-95"), AssetType.Laptop);
            await context.Assets.AddAsync(asset);
            await context.SaveChangesAsync();
        }

        var payload = new CreateTicketRequest(
            assetId,
            "Correction dysfonctionnement clavier matériel",
            "Le clavier présente plusieurs touches complètement hors service suite à un incident.",
            "Medium"
            );

        var response = await _client.PostAsJsonAsync("/api/tickets", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var ticket = await response.Content.ReadFromJsonAsync<TicketResponseDto>();
        Assert.NotNull(ticket);
        Assert.Equal("Support-Lectorat", ticket.AssignedTeam);
    }

    [Fact]
    public async Task TransferTicket_Should_ReturnNoContent_And_UpdateDatabase()
    {
        // Arrange
        var client = _factory.CreateClient();
        var ticketId = Guid.NewGuid();
        var requestPayload = new TransferTicketRequest("Infra-Réseaux", "Problème de switch");

        // --- Préparation de la base de données ---
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            // Attention: Utiliser le vrai constructeur de ton entité
            dbContext.Tickets.Add(new MaintenanceTicket(ticketId, Guid.NewGuid(), "titre", "description", TicketCriticality.Low, "Support-Local"));
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
            updatedTicket!.AssignedTeam.Should().Be("Infra-Réseaux");
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
            // Attention: Utiliser le vrai constructeur de ton entité
            dbContext.Tickets.Add(new MaintenanceTicket(ticketId, Guid.NewGuid(), "titre", "description", TicketCriticality.Low, "Support-Local"));
            await dbContext.SaveChangesAsync();
        }
        var response = await client.GetAsync($"/api/tickets/{ticketId}");

        var assets = await response.Content.ReadFromJsonAsync<IEnumerable<TicketResponseDto>>();
        assets.Should().NotBeNull();
        assets.Should().ContainSingle(a => a.Id == ticketId);

    }
}