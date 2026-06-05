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
