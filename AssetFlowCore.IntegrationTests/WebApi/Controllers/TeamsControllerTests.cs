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
    }
}
