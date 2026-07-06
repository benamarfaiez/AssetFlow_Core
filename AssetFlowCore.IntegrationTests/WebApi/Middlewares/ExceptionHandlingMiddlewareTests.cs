using AssetFlowCore.WebApi.Requests;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi.Middlewares;

public class ExceptionHandlingMiddlewareTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Request_WhenDomainExceptionIsThrown_ShouldReturn400WithProblemDetails()
    {
        // Arrange
        var uniqueSerial = $"SER-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        var payload = new RegisterAssetRequest("Machine de Test Middleware", uniqueSerial, "Server");

        var firstResponse = await _client.PostAsJsonAsync("/api/assets", payload);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Le premier enregistrement doit réussir pour initialiser le doublon.");

        // Act
        var response = await _client.PostAsJsonAsync("/api/assets", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "La base contient déjà ce numéro de série, une DomainException doit être levée.");

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Règle métier violée");
        problemDetails.Detail.Should().Contain("Ce numéro de série constructeur est déjà enregistré dans le parc.");
    }
}