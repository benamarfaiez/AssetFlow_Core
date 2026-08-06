using AssetFlowCore.Application.DTOs;
using AssetFlowCore.WebApi.Requests;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi.Controllers
{
    public class TeamsControllerTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly CustomWebApplicationFactory<Program> _factory = factory;

        [Fact]
        public async Task CreateTeam_ThenGetTeam_ShouldReturnCreatedAndThenOk()
        {
            // Arrange
            var payload = new CreateTeamRequest("Integration-Team", "Laptop", "High", "Desc Test");

            // Act: create
            var createResponse = await _client.PostAsJsonAsync("/api/v1/teams", payload);

            // Assert create
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content.ReadFromJsonAsync<TeamResponseDto>();
            created.Should().NotBeNull();
            created!.Name.Should().Be("Integration-Team");

            // Act: get
            var getResponse = await _client.GetAsync($"/api/v1/teams/{created.Id}");

            // Assert get
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var got = await getResponse.Content.ReadFromJsonAsync<TeamResponseDto>();
            got.Should().NotBeNull();
            got!.Id.Should().Be(created.Id);
        }

        [Fact]
        public async Task DeleteTeam_ShouldReturnNoContent_ThenNotFound()
        {
            // Arrange: create team
            var payload = new CreateTeamRequest("ToDelete", "Server", "High", "Desc");
            var createResponse = await _client.PostAsJsonAsync("/api/v1/teams", payload);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content.ReadFromJsonAsync<TeamResponseDto>();

            // Act: delete
            var deleteResponse = await _client.DeleteAsync($"/api/v1/teams/{created!.Id}");

            // Assert delete
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act : la ressource n'existe plus, le middleware traduit NotFoundException en 404
            var getAfter = await _client.GetAsync($"/api/v1/teams/{created.Id}");
            getAfter.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetTeam_NotFound_ShouldReturn404()
        {
            var unknownId = Guid.NewGuid();

            var resp = await _client.GetAsync($"/api/v1/teams/{unknownId}");

            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var problem = await resp.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
            problem!.Title.Should().Be("Ressource introuvable");
            problem.Detail.Should().Be($"L'équipe {unknownId} est introuvable.");
        }

        [Fact]
        public async Task CreateTeam_BadRequest_ShouldReturn400()
        {
            var payload = new CreateTeamRequest("", "", "", null);
            var resp = await _client.PostAsJsonAsync("/api/v1/teams", payload);
            // Creating with invalid data currently throws ArgumentException in handler -> 500
            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateTeam_BadRequest_ShouldReturn400()
        {
            // Arrange: Création d'une équipe valide au préalable
            var payload = new CreateTeamRequest("ToUpdateBad", "Server", "High", "Desc");
            var createResponse = await _client.PostAsJsonAsync("/api/v1/teams", payload);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content.ReadFromJsonAsync<TeamResponseDto>();

            // Act: Tentative de mise à jour avec un corps de texte invalide (chaînes vides)
            var update = new UpdateTeamRequest("", "", "", null);
            var updateResponse = await _client.PutAsJsonAsync($"/api/v1/teams/{created!.Id}", update);

            // Assert: Grâce au ValidationBehavior, l'API rejette proprement la requête avant le plantage du Domaine
            updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest); // Attend désormais une erreur 400 !
        }

        [Fact]
        public async Task UpdateTeam_NotFound_ShouldReturn404()
        {
            var update = new UpdateTeamRequest("Name", "Server", "High", "Desc");
            var resp = await _client.PutAsJsonAsync($"/api/v1/teams/{Guid.NewGuid()}", update);
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Couvre la correction 1.4 : le doublon de nom n'était détecté que par l'index unique
        /// de la base, donc restitué au client sous la forme d'une 500.
        /// </summary>
        [Fact]
        public async Task CreateTeam_WithDuplicateName_ShouldReturn400WithBusinessMessage()
        {
            // Arrange
            var payload = new CreateTeamRequest("Equipe-Doublon", "Server", "High", "Première création");
            var first = await _client.PostAsJsonAsync("/api/v1/teams", payload);
            first.StatusCode.Should().Be(HttpStatusCode.Created);

            // Act
            var duplicate = await _client.PostAsJsonAsync("/api/v1/teams", payload);

            // Assert
            duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problem = await duplicate.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
            problem.Should().NotBeNull();
            problem!.Title.Should().Be("Règle métier violée");
            problem.Detail.Should().Contain("Une équipe nommée 'Equipe-Doublon' existe déjà.");
        }

        [Fact]
        public async Task UpdateTeam_WithNameOfAnotherTeam_ShouldReturn400WithBusinessMessage()
        {
            // Arrange : deux équipes distinctes
            var existing = await _client.PostAsJsonAsync("/api/v1/teams",
                new CreateTeamRequest("Equipe-Occupee", "Server", "High", "Nom déjà pris"));
            existing.StatusCode.Should().Be(HttpStatusCode.Created);

            var toRename = await _client.PostAsJsonAsync("/api/v1/teams",
                new CreateTeamRequest("Equipe-A-Renommer", "Laptop", "Low", "À renommer"));
            toRename.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await toRename.Content.ReadFromJsonAsync<TeamResponseDto>();

            // Act
            var response = await _client.PutAsJsonAsync($"/api/v1/teams/{created!.Id}",
                new UpdateTeamRequest("Equipe-Occupee", "Laptop", "Low", "À renommer"));

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
            problem!.Detail.Should().Contain("Une équipe nommée 'Equipe-Occupee' existe déjà.");
        }

        [Fact]
        public async Task UpdateTeam_ShouldReturnUpdated()
        {
            // Arrange: create first
            var payload = new CreateTeamRequest("ToUpdate", "Server", "High", "Desc");
            var createResponse = await _client.PostAsJsonAsync("/api/v1/teams", payload);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content.ReadFromJsonAsync<TeamResponseDto>();

            // Act: update
            var update = new UpdateTeamRequest("UpdatedName", "Laptop", "Low", "NewDesc");
            var updateResponse = await _client.PutAsJsonAsync($"/api/v1/teams/{created!.Id}", update);

            // Assert : une mise à jour répond 200, plus 201
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var updated = await updateResponse.Content.ReadFromJsonAsync<TeamResponseDto>();
            updated.Should().NotBeNull();
            updated!.Name.Should().Be("UpdatedName");
            updated.Description.Should().Be("NewDesc");
            // Le DTO expose désormais le couple de routage : le formulaire peut se préremplir
            updated.AssetType.Should().Be("Laptop");
            updated.TicketCriticality.Should().Be("Low");
        }
    }
}
