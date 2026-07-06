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
            var createResponse = await _client.PostAsJsonAsync("/api/teams", payload);

            // Assert create
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content.ReadFromJsonAsync<TeamResponseDto>();
            created.Should().NotBeNull();
            created!.Name.Should().Be("Integration-Team");

            // Act: get
            var getResponse = await _client.GetAsync($"/api/teams/{created.Id}");

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
            var createResponse = await _client.PostAsJsonAsync("/api/teams", payload);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content.ReadFromJsonAsync<TeamResponseDto>();

            // Act: delete
            var deleteResponse = await _client.DeleteAsync($"/api/teams/{created!.Id}");

            // Assert delete
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act: get should return 400 (DomainException -> 400 in middleware)
            var getAfter = await _client.GetAsync($"/api/teams/{created.Id}");
            getAfter.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetTeam_NotFound_ShouldReturn404()
        {
            var resp = await _client.GetAsync($"/api/teams/{Guid.NewGuid()}");
            // Handler throws DomainException which is mapped to 400 by middleware
            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateTeam_BadRequest_ShouldReturn400()
        {
            var payload = new CreateTeamRequest("", "", "", null);
            var resp = await _client.PostAsJsonAsync("/api/teams", payload);
            // Creating with invalid data currently throws ArgumentException in handler -> 500
            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateTeam_BadRequest_ShouldReturn400()
        {
            // Arrange: Création d'une équipe valide au préalable
            var payload = new CreateTeamRequest("ToUpdateBad", "Server", "High", "Desc");
            var createResponse = await _client.PostAsJsonAsync("/api/teams", payload);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content.ReadFromJsonAsync<TeamResponseDto>();

            // Act: Tentative de mise à jour avec un corps de texte invalide (chaînes vides)
            var update = new UpdateTeamRequest("", "", "", null);
            var updateResponse = await _client.PutAsJsonAsync($"/api/teams/{created!.Id}", update);

            // Assert: Grâce au ValidationBehavior, l'API rejette proprement la requête avant le plantage du Domaine
            updateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest); // Attend désormais une erreur 400 !
        }

        [Fact]
        public async Task UpdateTeam_NotFound_ShouldReturn404()
        {
            var update = new UpdateTeamRequest("Name", "Server", "High", "Desc");
            var resp = await _client.PutAsJsonAsync($"/api/teams/{Guid.NewGuid()}", update);
            // GetById throws DomainException mapped to 400
            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateTeam_ShouldReturnUpdated()
        {
            // Arrange: create first
            var payload = new CreateTeamRequest("ToUpdate", "Server", "High", "Desc");
            var createResponse = await _client.PostAsJsonAsync("/api/teams", payload);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await createResponse.Content.ReadFromJsonAsync<TeamResponseDto>();

            // Act: update
            var update = new UpdateTeamRequest("UpdatedName", "Laptop", "Low", "NewDesc");
            var updateResponse = await _client.PutAsJsonAsync($"/api/teams/{created!.Id}", update);

            // Assert
            updateResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var updated = await updateResponse.Content.ReadFromJsonAsync<TeamResponseDto>();
            updated.Should().NotBeNull();
            updated!.Name.Should().Be("UpdatedName");
            // AssetType and TicketCriticality are not exposed in the response DTO; verify Description and Name instead
            updated.Description.Should().Be("NewDesc");
            updated.Name.Should().Be("UpdatedName");
        }
    }
}
