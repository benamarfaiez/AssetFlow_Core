using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.WebApi.Requests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi.Controllers;

/// <summary>
/// Couvre l'étape 2.2 : liste des équipes, avec ou sans les équipes désactivées.
/// L'équipe désactivée est amorcée directement en base, aucun endpoint ne permettant
/// aujourd'hui de désactiver une équipe (décision 0.6 non tranchée).
/// </summary>
public class TeamsListTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TeamsListTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
        db.Database.EnsureDeleted();

        var active = new Team("Liste-Equipe-Active", "Server", "High", "Astreinte serveurs");
        var desactivee = new Team("Liste-Equipe-Desactivee", "Laptop", "Low", "Équipe dissoute");
        desactivee.Deactivate();
        db.Teams.AddRange(active, desactivee);
        db.SaveChanges();
    }

    [Fact]
    public async Task GetTeams_ShouldReturnEveryTeam_IncludingDeactivatedOnes()
    {
        var response = await _client.GetAsync("/api/teams");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teams = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<TeamResponseDto>>();

        teams.Should().HaveCount(2);
        teams!.Select(t => t.Name).Should().ContainInOrder("Liste-Equipe-Active", "Liste-Equipe-Desactivee");
        teams.Should().ContainSingle(t => !t.IsActive);
    }

    [Fact]
    public async Task GetTeams_OnlyActive_ShouldExcludeDeactivatedTeams()
    {
        var response = await _client.GetAsync("/api/teams?onlyActive=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teams = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<TeamResponseDto>>();

        teams.Should().ContainSingle()
             .Which.Name.Should().Be("Liste-Equipe-Active");
    }

    [Fact]
    public async Task GetTeams_ShouldExposeTheRoutingCouple()
    {
        var teams = await _client.GetFromJsonAsync<IReadOnlyCollection<TeamResponseDto>>("/api/teams?onlyActive=true");

        // Sans ce couple, l'écran d'administration ne peut ni préremplir un formulaire
        // ni signaler les combinaisons (type × criticité) non couvertes.
        var equipe = teams.Should().ContainSingle().Subject;
        equipe.AssetType.Should().Be("Server");
        equipe.TicketCriticality.Should().Be("High");
    }

    [Fact]
    public async Task GetTeams_ShouldReflectACreationImmediately()
    {
        // Amorce le cache de liste avant l'écriture.
        (await _client.GetFromJsonAsync<IReadOnlyCollection<TeamResponseDto>>("/api/teams"))!.Should().HaveCount(2);

        var created = await _client.PostAsJsonAsync("/api/teams",
            new CreateTeamRequest("Liste-Equipe-Nouvelle", "NetworkDevice", "Medium", "Réseau"));
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var teams = await _client.GetFromJsonAsync<IReadOnlyCollection<TeamResponseDto>>("/api/teams");

        teams.Should().HaveCount(3);
        teams!.Should().ContainSingle(t => t.Name == "Liste-Equipe-Nouvelle");
    }
}
