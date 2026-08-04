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

/// <summary>
/// Couvre l'étape 2.8 : toute réponse 201 doit porter un en-tête <c>Location</c> désignant la
/// ressource créée, et cette adresse doit être réellement suivable.
/// </summary>
public class LocationHeaderTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public LocationHeaderTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateAsset_ShouldReturnLocationOfTheCreatedAsset()
    {
        var response = await _client.PostAsJsonAsync("/api/assets",
            new RegisterAssetRequest("Serveur Location", "LOC-SRV-01", "Server"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<AssetResponseDto>();

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/Assets/{created!.Id}");

        // L'adresse annoncée doit mener à la ressource.
        var suivi = await _client.GetAsync(response.Headers.Location);
        suivi.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateTeam_ShouldReturnLocationOfTheCreatedTeam()
    {
        var response = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest("Equipe-Location", "Laptop", "Medium", "Support"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TeamResponseDto>();

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/Teams/{created!.Id}");

        var suivi = await _client.GetAsync(response.Headers.Location);
        suivi.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateTicket_ShouldReturnLocationOfTheCreatedTicket()
    {
        var assetId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            db.Assets.Add(new Asset(assetId, "Poste Location", SerialNumber.Create("LOC-LPT-01"), AssetType.Laptop));
            db.Teams.Add(new Team("Equipe-Location-Incident", AssetType.Laptop.ToString(), TicketCriticality.Medium.ToString(), "Support"));
            await db.SaveChangesAsync();
        }

        var response = await _client.PostAsJsonAsync("/api/tickets",
            new CreateTicketRequest(assetId, "Écran noir", "Le poste ne s'allume plus.", "Medium"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TicketResponseDto>();

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.AbsolutePath.Should().Be($"/api/Tickets/{created!.Id}");

        var suivi = await _client.GetAsync(response.Headers.Location);
        suivi.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
