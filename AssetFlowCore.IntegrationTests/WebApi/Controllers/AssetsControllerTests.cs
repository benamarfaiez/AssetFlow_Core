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

public class AssetsControllerTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_WithValidPayload_ShouldReturnCreatedAndPersist()
    {
        // Arrange
        var payload = new RegisterAssetRequest("Serveur-Web-Test", "SRV-WEB-99", "Server");

        // Act
        var response = await _client.PostAsJsonAsync("/api/assets", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AssetResponseDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Serveur-Web-Test");
        body.Status.Should().Be("InService");
    }

    [Fact]
    public async Task GetAll_ShouldReturnSuccessStatusCodeAndDeterministicList()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            context.Assets.RemoveRange(context.Assets); // Nettoyage pour isolation

            var asset = new Asset(assetId, "Switch Intégration", SerialNumber.Create("SWI-GET-ALL"), AssetType.NetworkDevice);
            await context.Assets.AddAsync(asset);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/assets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var assets = await response.Content.ReadFromJsonAsync<IEnumerable<AssetResponseDto>>();
        assets.Should().NotBeNull();
        assets.Should().ContainSingle(a => a.Id == assetId && a.Name == "Switch Intégration");
    }

    [Fact]
    public async Task Decommission_WithValidAsset_ShouldReturnNoContentAndUpdateStatus()
    {
        // Arrange: create asset in DB
        var assetId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            context.Assets.RemoveRange(context.Assets);

            var asset = new Asset(assetId, "To Decommission", SerialNumber.Create("DECOM-1"), AssetType.Server);
            await context.Assets.AddAsync(asset);
            await context.SaveChangesAsync();
        }

        // Act
        var resp = await _client.PutAsync($"/api/assets/{assetId}/decommission", null);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            var updated = await context.Assets.FindAsync(assetId);
            updated.Should().NotBeNull();
            updated!.Status.Should().Be(Domain.Enums.AssetStatus.Decommissioned);
        }
    }

    [Fact]
    public async Task Decommission_NotFound_ShouldReturnNotFound()
    {
        var unknownId = Guid.NewGuid();

        // Act
        var resp = await _client.PutAsync($"/api/assets/{unknownId}/decommission", null);

        // Assert : NotFoundException est traduite en 404 par le middleware
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await resp.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problem!.Detail.Should().Be($"L'actif {unknownId} est introuvable.");
    }

    [Fact]
    public async Task Decommission_WithActiveTickets_ShouldReturnBadRequest()
    {
        // Arrange: create asset, team and an active ticket
        var assetId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            context.Assets.RemoveRange(context.Assets);
            context.Tickets.RemoveRange(context.Tickets);
            context.Teams.RemoveRange(context.Teams);

            var asset = new Asset(assetId, "With Tickets", SerialNumber.Create("TKT-1"), AssetType.Server);
            await context.Assets.AddAsync(asset);

            var team = new Team("Support", "Server", "High", "Support team");
            await context.Teams.AddAsync(team);

            var ticket = new MaintenanceTicket(Guid.NewGuid(), assetId, "Fail", "Broken", TicketCriticality.High, team.Id);
            await context.Tickets.AddAsync(ticket);

            await context.SaveChangesAsync();
        }

        // Act
        var resp = await _client.PutAsync($"/api/assets/{assetId}/decommission", null);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}