using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using AssetFlowCore.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi.Controllers;

/// <summary>
/// Couvre l'étape 2.5 : fiche d'un actif et de ses incidents, jusqu'ici impossible sans
/// parcourir l'inventaire complet.
/// </summary>
public class AssetDetailTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly Guid _actifAvecIncidents = Guid.NewGuid();
    private readonly Guid _actifSansIncident = Guid.NewGuid();

    public AssetDetailTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
        db.Database.EnsureDeleted();

        var avecIncidents = new Asset(_actifAvecIncidents, "Serveur de fichiers", SerialNumber.Create("FIC-SRV-01"), AssetType.Server);
        var sansIncident = new Asset(_actifSansIncident, "Commutateur neuf", SerialNumber.Create("FIC-NET-01"), AssetType.NetworkDevice);
        db.Assets.AddRange(avecIncidents, sansIncident);

        var equipe = new Team("Fiche-Serveurs", "Server", "High", "Astreinte serveurs");
        db.Teams.Add(equipe);

        var ancien = new MaintenanceTicket(Guid.NewGuid(), _actifAvecIncidents, "Ventilateur bruyant", "Bruit anormal.", TicketCriticality.Low, equipe.Id);
        var recent = new MaintenanceTicket(Guid.NewGuid(), _actifAvecIncidents, "Disque saturé", "Volume plein.", TicketCriticality.High, equipe.Id);
        db.Tickets.AddRange(ancien, recent);
        db.SaveChanges();

        // Désynchronise les dates d'ouverture pour vérifier l'ordre de restitution.
        db.Entry(ancien).Property(t => t.CreatedAt).CurrentValue = DateTime.UtcNow.AddDays(-3);
        db.SaveChanges();
    }

    [Fact]
    public async Task GetAsset_ShouldReturnTheAssetAndItsTickets_MostRecentFirst()
    {
        var response = await _client.GetAsync($"/api/v1/assets/{_actifAvecIncidents}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var fiche = await response.Content.ReadFromJsonAsync<AssetDetailResponseDto>();

        fiche.Should().NotBeNull();
        fiche!.Id.Should().Be(_actifAvecIncidents);
        fiche.Name.Should().Be("Serveur de fichiers");
        fiche.SerialNumber.Should().Be("FIC-SRV-01");
        fiche.Type.Should().Be("Server");
        fiche.Status.Should().Be("InService");

        fiche.Tickets.Should().HaveCount(2);
        fiche.Tickets.Select(t => t.Title).Should().ContainInOrder("Disque saturé", "Ventilateur bruyant");
        fiche.Tickets.Should().OnlyContain(t => t.AssignedTeamName == "Fiche-Serveurs");
    }

    [Fact]
    public async Task GetAsset_WithoutTicket_ShouldReturnAnEmptyCollection()
    {
        var fiche = await _client.GetFromJsonAsync<AssetDetailResponseDto>($"/api/v1/assets/{_actifSansIncident}");

        fiche.Should().NotBeNull();
        // Une collection vide, jamais null : le client n'a pas à distinguer les deux cas.
        fiche!.Tickets.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetAsset_Unknown_ShouldReturn404()
    {
        var unknownId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/v1/assets/{unknownId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problem!.Title.Should().Be("Ressource introuvable");
        problem.Detail.Should().Be($"L'actif {unknownId} est introuvable.");
    }

    [Fact]
    public async Task GetAsset_WithMalformedIdentifier_ShouldReturn404FromRouting()
    {
        // La contrainte de route {id:guid} rejette la valeur avant d'atteindre le cas d'usage.
        var response = await _client.GetAsync("/api/v1/assets/pas-un-guid");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
