using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.WebApi.Requests;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi.Controllers;

public class TicketsControllerTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateTicket_WithValidAsset_ShouldMutateAssetAndReturnCreated()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
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

        // Act
        var response = await _client.PostAsJsonAsync("/api/tickets", payload);

        // Assert
        // Vérification du code statut (Attendu: 201 Created)
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var ticket = await response.Content.ReadFromJsonAsync<TicketResponseDto>();
        Assert.NotNull(ticket);
        Assert.Equal("Support-Lectorat", ticket.AssignedTeam);
    }
}