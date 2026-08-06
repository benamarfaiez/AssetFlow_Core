using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.WebApi.Authorization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi.Controllers;

/// <summary>
/// Couvre l'étape 2b.4 (Lot 2 bis, décision 0.6) : activation / désactivation d'une équipe,
/// réservées au rôle Administrateur, avec invalidation des deux listes en cache.
/// </summary>
public class TeamActivationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    private HttpClient CreateClientWithRoles(params string[] roles)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(",", roles));
        return client;
    }

    private async Task<Team> SeedTeamAsync(string name, bool active = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
        var team = new Team(name, "Server", "High", "Description");
        if (!active) team.Deactivate();
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return team;
    }

    [Fact]
    public async Task Deactivate_ShouldFlipIsActive_AndExcludeTeamFromOnlyActiveFilter()
    {
        var team = await SeedTeamAsync("Equipe-A-Desactiver");

        // Amorce le cache des deux listes avant l'écriture.
        await _client.GetFromJsonAsync<IReadOnlyCollection<TeamResponseDto>>("/api/v1/teams?onlyActive=true");
        await _client.GetFromJsonAsync<IReadOnlyCollection<TeamResponseDto>>("/api/v1/teams?onlyActive=false");

        var response = await _client.PutAsync($"/api/v1/teams/{team.Id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<TeamResponseDto>();
        updated!.IsActive.Should().BeFalse();

        var onlyActive = await _client.GetFromJsonAsync<IReadOnlyCollection<TeamResponseDto>>("/api/v1/teams?onlyActive=true");
        onlyActive!.Should().NotContain(t => t.Id == team.Id);

        var all = await _client.GetFromJsonAsync<IReadOnlyCollection<TeamResponseDto>>("/api/v1/teams?onlyActive=false");
        all!.Should().ContainSingle(t => t.Id == team.Id && !t.IsActive);
    }

    [Fact]
    public async Task Activate_ShouldFlipIsActive_AndReincludeTeamInOnlyActiveFilter()
    {
        var team = await SeedTeamAsync("Equipe-A-Reactiver", active: false);

        var response = await _client.PutAsync($"/api/v1/teams/{team.Id}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<TeamResponseDto>();
        updated!.IsActive.Should().BeTrue();

        var onlyActive = await _client.GetFromJsonAsync<IReadOnlyCollection<TeamResponseDto>>("/api/v1/teams?onlyActive=true");
        onlyActive!.Should().ContainSingle(t => t.Id == team.Id);
    }

    [Theory]
    [InlineData("activate")]
    [InlineData("deactivate")]
    public async Task ActivateOrDeactivate_UnknownTeam_ShouldReturn404(string action)
    {
        var response = await _client.PutAsync($"/api/v1/teams/{Guid.NewGuid()}/{action}", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("activate")]
    [InlineData("deactivate")]
    public async Task ActivateOrDeactivate_AsNonAdministrator_ShouldReturn403(string action)
    {
        var team = await SeedTeamAsync($"Equipe-403-{action}");
        var client = CreateClientWithRoles(Roles.Technicien);

        var response = await client.PutAsync($"/api/v1/teams/{team.Id}/{action}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
