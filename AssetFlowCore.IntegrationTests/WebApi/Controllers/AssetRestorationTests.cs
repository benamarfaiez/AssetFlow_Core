using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.WebApi.Authorization;
using AssetFlowCore.WebApi.Requests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi.Controllers;

/// <summary>
/// Couvre l'étape 2b.5 (Lot 2 bis, décision 0.4) : remise en service d'un actif mis au rebut,
/// motif obligatoire, réservée au rôle Administrateur.
/// </summary>
public class AssetRestorationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    private HttpClient CreateClientWithRoles(params string[] roles)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(",", roles));
        return client;
    }

    private async Task<Guid> SeedDecommissionedAssetAsync(string serialNumber)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
        var asset = new Asset(Guid.NewGuid(), "Actif rebuté", SerialNumber.Create(serialNumber), AssetType.Laptop);
        asset.Decommission();
        await context.Assets.AddAsync(asset);
        await context.SaveChangesAsync();
        return asset.Id;
    }

    [Fact]
    public async Task RestoreToService_WithReason_ShouldReturnNoContent_AndAssetBecomesEligibleForNewTicket()
    {
        var assetId = await SeedDecommissionedAssetAsync("RESTORE-1");

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/assets/{assetId}/restore-to-service",
            new RestoreAssetToServiceRequest("Mise au rebut réalisée par erreur"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            var updated = await context.Assets.FindAsync(assetId);
            updated!.Status.Should().Be(AssetStatus.InService);
        }

        // L'actif redevient éligible à l'ouverture d'un incident : plus de blocage sur Decommissioned.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            var team = new Team("Equipe-Restauration", AssetType.Laptop.ToString(), TicketCriticality.Low.ToString(), "Desc");
            await context.Teams.AddAsync(team);
            await context.SaveChangesAsync();
        }

        var createTicket = await _client.PostAsJsonAsync(
            "/api/v1/tickets",
            new CreateTicketRequest(assetId, "Titre", "Description", "Low"));
        createTicket.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RestoreToService_UnknownAsset_ShouldReturn404()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/v1/assets/{Guid.NewGuid()}/restore-to-service",
            new RestoreAssetToServiceRequest("Motif"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestoreToService_WithBlankReason_ShouldReturn400()
    {
        var assetId = await SeedDecommissionedAssetAsync("RESTORE-2");

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/assets/{assetId}/restore-to-service",
            new RestoreAssetToServiceRequest("   "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RestoreToService_WhenNotDecommissioned_ShouldReturn400()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
        var asset = new Asset(Guid.NewGuid(), "Actif en service", SerialNumber.Create("RESTORE-3"), AssetType.Laptop);
        await context.Assets.AddAsync(asset);
        await context.SaveChangesAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/assets/{asset.Id}/restore-to-service",
            new RestoreAssetToServiceRequest("Motif"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RestoreToService_AsNonAdministrator_ShouldReturn403()
    {
        var assetId = await SeedDecommissionedAssetAsync("RESTORE-4");
        var client = CreateClientWithRoles(Roles.Technicien);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/assets/{assetId}/restore-to-service",
            new RestoreAssetToServiceRequest("Motif"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
