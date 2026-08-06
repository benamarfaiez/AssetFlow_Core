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
/// Couvre l'étape 2.1 : liste d'incidents filtrable, triable et paginée. Le jeu de données est
/// amorcé une seule fois pour la classe, les tests étant tous en lecture.
/// </summary>
public class TicketsListTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly Guid _serveurId = Guid.NewGuid();
    private readonly Guid _portableId = Guid.NewGuid();
    private Guid _equipeServeursId;
    private Guid _equipeSupportId;

    public TicketsListTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        Seed(factory);
    }

    private void Seed(CustomWebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
        db.Database.EnsureDeleted();

        var serveur = new Asset(_serveurId, "Serveur applicatif", SerialNumber.Create("LST-SRV-01"), AssetType.Server);
        var portable = new Asset(_portableId, "Poste nomade", SerialNumber.Create("LST-LPT-01"), AssetType.Laptop);
        db.Assets.AddRange(serveur, portable);

        var equipeServeurs = new Team("Liste-Serveurs", "Server", "High", "Astreinte serveurs");
        var equipeSupport = new Team("Liste-Support", "Laptop", "Low", "Support de proximité");
        db.Teams.AddRange(equipeServeurs, equipeSupport);
        _equipeServeursId = equipeServeurs.Id;
        _equipeSupportId = equipeSupport.Id;

        // 5 incidents : 3 sur le serveur (dont 1 pris en charge), 2 sur le portable.
        var incidents = new[]
        {
            new MaintenanceTicket(Guid.NewGuid(), _serveurId, "Disque saturé", "Volume système plein.", TicketCriticality.High, equipeServeurs.Id),
            new MaintenanceTicket(Guid.NewGuid(), _serveurId, "Redémarrages", "Redémarrages inopinés.", TicketCriticality.Medium, equipeServeurs.Id),
            new MaintenanceTicket(Guid.NewGuid(), _serveurId, "Sauvegarde en échec", "La sauvegarde nocturne échoue.", TicketCriticality.Low, equipeServeurs.Id),
            new MaintenanceTicket(Guid.NewGuid(), _portableId, "Clavier HS", "Plusieurs touches inertes.", TicketCriticality.Medium, equipeSupport.Id),
            new MaintenanceTicket(Guid.NewGuid(), _portableId, "Batterie", "Autonomie réduite à 20 minutes.", TicketCriticality.Low, equipeSupport.Id)
        };
        incidents[0].AssignToTechnician(Guid.NewGuid());
        db.Tickets.AddRange(incidents);

        db.SaveChanges();
    }

    private async Task<PagedResultDto<TicketResponseDto>> GetPageAsync(string queryString)
    {
        var response = await _client.GetAsync($"/api/tickets{queryString}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResultDto<TicketResponseDto>>();
        page.Should().NotBeNull();
        return page!;
    }

    [Fact]
    public async Task GetTickets_WithoutFilter_ShouldReturnEveryTicketWithTotalCount()
    {
        var page = await GetPageAsync(string.Empty);

        page.TotalCount.Should().Be(5);
        page.Items.Should().HaveCount(5);
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(20);
        page.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetTickets_ShouldExposeTheEnrichedFields()
    {
        var page = await GetPageAsync("?assetId=" + _serveurId + "&criticality=High");

        var incident = page.Items.Should().ContainSingle().Subject;
        incident.Title.Should().Be("Disque saturé");
        incident.Description.Should().Be("Volume système plein.");
        incident.Status.Should().Be("InProgress");
        incident.AssignedTeamName.Should().Be("Liste-Serveurs");
        incident.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
        incident.ResolutionComment.Should().BeNull();
        incident.IsAiProcessing.Should().BeTrue();
    }

    [Theory]
    [InlineData("?status=Opened", 4)]
    [InlineData("?status=InProgress", 1)]
    [InlineData("?status=Closed", 0)]
    [InlineData("?criticality=Low", 2)]
    [InlineData("?criticality=high", 1)]
    public async Task GetTickets_ShouldApplyEachFilter(string queryString, int attendu)
    {
        var page = await GetPageAsync(queryString);

        page.TotalCount.Should().Be(attendu);
        page.Items.Should().HaveCount(attendu);
    }

    [Fact]
    public async Task GetTickets_ShouldFilterByTeamAndAsset()
    {
        var parEquipe = await GetPageAsync($"?teamId={_equipeSupportId}");
        parEquipe.TotalCount.Should().Be(2);
        parEquipe.Items.Should().OnlyContain(t => t.AssignedTeamId == _equipeSupportId);

        var parActif = await GetPageAsync($"?assetId={_serveurId}");
        parActif.TotalCount.Should().Be(3);
        parActif.Items.Should().OnlyContain(t => t.AssetId == _serveurId);

        var croise = await GetPageAsync($"?teamId={_equipeServeursId}&assetId={_portableId}");
        croise.TotalCount.Should().Be(0);
        croise.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTickets_SortedByCriticality_ShouldUseBusinessOrderNotAlphabeticalOrder()
    {
        // Un tri alphabétique sur la colonne texte donnerait High, Low, Medium.
        var descendant = await GetPageAsync($"?assetId={_serveurId}&sortBy=Criticality&sortDescending=true");
        descendant.Items.Select(t => t.Criticality).Should().ContainInOrder("High", "Medium", "Low");

        var ascendant = await GetPageAsync($"?assetId={_serveurId}&sortBy=Criticality&sortDescending=false");
        ascendant.Items.Select(t => t.Criticality).Should().ContainInOrder("Low", "Medium", "High");
    }

    [Fact]
    public async Task GetTickets_SortedByTitle_ShouldRespectRequestedDirection()
    {
        var page = await GetPageAsync($"?assetId={_portableId}&sortBy=Title&sortDescending=false");

        page.Items.Select(t => t.Title).Should().ContainInOrder("Batterie", "Clavier HS");
    }

    [Fact]
    public async Task GetTickets_ShouldPaginateWithoutLosingOrDuplicatingItems()
    {
        var premiere = await GetPageAsync("?page=1&pageSize=2&sortBy=Title&sortDescending=false");
        var deuxieme = await GetPageAsync("?page=2&pageSize=2&sortBy=Title&sortDescending=false");
        var troisieme = await GetPageAsync("?page=3&pageSize=2&sortBy=Title&sortDescending=false");

        premiere.TotalCount.Should().Be(5);
        premiere.TotalPages.Should().Be(3);
        premiere.Items.Should().HaveCount(2);
        deuxieme.Items.Should().HaveCount(2);
        troisieme.Items.Should().HaveCount(1);

        var identifiants = premiere.Items.Concat(deuxieme.Items).Concat(troisieme.Items).Select(t => t.Id).ToList();
        identifiants.Should().OnlyHaveUniqueItems().And.HaveCount(5);
    }

    [Fact]
    public async Task GetTickets_BeyondLastPage_ShouldReturnEmptyPageWithTotalCount()
    {
        var page = await GetPageAsync("?page=42&pageSize=20");

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(5);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?status=Inconnu")]
    [InlineData("?criticality=Urgent")]
    [InlineData("?sortBy=Couleur")]
    public async Task GetTickets_WithInvalidParameter_ShouldReturn400(string queryString)
    {
        var response = await _client.GetAsync($"/api/tickets{queryString}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        problem!.Title.Should().Be("Validation de la requête échouée");
    }
}
