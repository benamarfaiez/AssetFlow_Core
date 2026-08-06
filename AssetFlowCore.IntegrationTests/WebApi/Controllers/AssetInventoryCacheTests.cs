using AssetFlowCore.Application.DTOs;
using AssetFlowCore.WebApi.Requests;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi.Controllers;

/// <summary>
/// Couvre la correction 1.1 : les écritures passées par l'unité de travail doivent traverser
/// les décorateurs de cache. Chaque test amorce délibérément le cache d'inventaire par une
/// première lecture avant d'écrire — sans invalidation, la lecture suivante servait
/// l'inventaire périmé pendant 5 minutes.
/// </summary>
public class AssetInventoryCacheTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_AfterInventoryWasCached_ShouldAppearInImmediateInventoryRead()
    {
        // Arrange : première lecture pour peupler le cache d'inventaire
        var warmUp = await _client.GetAsync("/api/v1/assets");
        warmUp.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = new RegisterAssetRequest("Serveur-Cache-01", "SRV-CACHE-01", "Server");

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/v1/assets", payload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<AssetResponseDto>();

        var inventory = await _client.GetFromJsonAsync<IEnumerable<AssetResponseDto>>("/api/v1/assets");

        // Assert
        created.Should().NotBeNull();
        inventory.Should().NotBeNull();
        inventory!.Should().ContainSingle(a => a.Id == created!.Id)
            .Which.Name.Should().Be("Serveur-Cache-01");
    }

    [Fact]
    public async Task Decommission_AfterInventoryWasCached_ShouldExposeNewStatusOnImmediateRead()
    {
        // Arrange : actif créé puis inventaire relu, ce qui remet la liste en cache
        var payload = new RegisterAssetRequest("Serveur-Cache-02", "SRV-CACHE-02", "Server");
        var createResponse = await _client.PostAsJsonAsync("/api/v1/assets", payload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<AssetResponseDto>();
        created.Should().NotBeNull();

        var cachedInventory = await _client.GetFromJsonAsync<IEnumerable<AssetResponseDto>>("/api/v1/assets");
        cachedInventory!.Single(a => a.Id == created!.Id).Status.Should().Be("InService");

        // Act
        var decommissionResponse = await _client.PutAsync($"/api/v1/assets/{created!.Id}/decommission", null);
        decommissionResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var inventory = await _client.GetFromJsonAsync<IEnumerable<AssetResponseDto>>("/api/v1/assets");

        // Assert
        inventory.Should().NotBeNull();
        inventory!.Single(a => a.Id == created.Id).Status.Should().Be("Decommissioned");
    }
}
