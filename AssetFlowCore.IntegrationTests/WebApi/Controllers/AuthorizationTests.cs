using AssetFlowCore.Application.DTOs;
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
/// Couvre le Lot 7 (sécurité) : aucun endpoint accessible anonymement (401), les écritures
/// d'équipes réservées au rôle administrateur (403 pour un autre rôle), et la traçabilité de
/// l'auteur d'une prise en charge / d'une clôture (décision 0.2).
/// </summary>
public class AuthorizationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    private HttpClient CreateAnonymousClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UnauthenticatedHeader, "true");
        return client;
    }

    private HttpClient CreateClientWithRoles(params string[] roles)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(",", roles));
        return client;
    }

    [Theory]
    [InlineData("GET", "/api/assets")]
    [InlineData("GET", "/api/tickets")]
    [InlineData("GET", "/api/teams")]
    public async Task AnonymousRequest_OnProtectedEndpoint_ShouldReturnUnauthorized(string method, string path)
    {
        var client = CreateAnonymousClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnonymousNegotiate_OnTicketHub_ShouldReturnUnauthorized()
    {
        var client = CreateAnonymousClient();

        var response = await client.PostAsync("/ticketHub/negotiate?negotiateVersion=1", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTeam_AsNonAdministrator_ShouldReturnForbidden()
    {
        var client = CreateClientWithRoles(Roles.Technicien);
        var payload = new CreateTeamRequest("Équipe-403", "Server", "Low", "Créée par un rôle non habilité");

        var response = await client.PostAsJsonAsync("/api/teams", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateTeam_AsAdministrator_ShouldReturnCreated()
    {
        var client = CreateClientWithRoles(Roles.Administrateur);
        var payload = new CreateTeamRequest("Équipe-201-Admin", "Server", "Low", "Créée par un administrateur");

        var response = await client.PostAsJsonAsync("/api/teams", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AssignAndClose_ShouldRecordAuthorIdentity()
    {
        var assetId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            await context.Database.EnsureDeletedAsync();
            var asset = new Asset(assetId, "Poste traçabilité", SerialNumber.Create("TRACE-01"), AssetType.Laptop);
            var team = new Team("Équipe-Traçabilité", AssetType.Laptop.ToString(), TicketCriticality.Low.ToString(), "Description");
            await context.Assets.AddAsync(asset);
            await context.Teams.AddAsync(team);
            await context.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(
            "/api/tickets",
            new CreateTicketRequest(assetId, "Titre", "Description de l'incident", "Low"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponseDto>();
        created.Should().NotBeNull();

        var assignResponse = await client.PutAsync($"/api/tickets/{created!.Id}/assign", null);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var closeResponse = await client.PutAsJsonAsync(
            $"/api/tickets/{created.Id}/close",
            new CloseTicketRequest("Résolu."));
        closeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/tickets/{created.Id}");
        var ticket = await getResponse.Content.ReadFromJsonAsync<TicketResponseDto>();

        ticket.Should().NotBeNull();
        ticket!.AssignedByUserId.Should().NotBeNull().And.NotBe(Guid.Empty);
        ticket.ClosedByUserId.Should().NotBeNull().And.NotBe(Guid.Empty);
        ticket.AssignedByUserId.Should().Be(ticket.ClosedByUserId, "le même utilisateur de test authentifie toute la requête");
    }
}
