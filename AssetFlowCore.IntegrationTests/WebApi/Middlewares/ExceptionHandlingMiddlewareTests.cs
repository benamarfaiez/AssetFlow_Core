using AssetFlowCore.WebApi.Controllers;
using AssetFlowCore.WebApi.Requests;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi.Middlewares;

public class ExceptionHandlingMiddlewareTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExceptionHandlingMiddlewareTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Request_WhenDomainExceptionIsThrown_ShouldReturn400WithProblemDetails()
    {
        // Arrange
        var serial = "DUPLICATE-VALID-1";
        var payload = new RegisterAssetRequest("Machine unique", serial, "Server");

        // Premier enregistrement
        await _client.PostAsJsonAsync("/api/assets", payload);

        // Act : Deuxième enregistrement provoquant la DomainException pour doublon
        var response = await _client.PostAsJsonAsync("/api/assets", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Erreur applicative");
        problemDetails.Detail.Should().Contain("Série déjà enregistrée");
    }
}