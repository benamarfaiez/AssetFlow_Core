using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi;

/// <summary>
/// Couvre la correction 1.2 : les sondes interrogées par le HEALTHCHECK du Dockerfile et de
/// docker-compose.yml doivent répondre hors Development, l'image tournant en Production.
/// </summary>
public class HealthEndpointsTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Probes_ShouldRespondOk_InProductionEnvironment(string path)
    {
        // Arrange : environnement identique à celui de l'image conteneurisée
        using var client = _factory
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"))
            .CreateClient();

        // Act
        var response = await client.GetAsync(path);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }
}
