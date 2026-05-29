using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.ValueObjects;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.WebApi.Controllers;
using AssetFlowCore.WebApi.Requests;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi.Controllers;

public class AssetsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public AssetsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidPayload_ShouldReturnCreatedAndPersist()
    {
        // Arrange
        var payload = new RegisterAssetRequest("Serveur-Web-Test", "SRV-WEB-99", "Server");

        // Act
        var response = await _client.PostAsJsonAsync("/api/assets", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AssetResponseDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Serveur-Web-Test");
        body.Status.Should().Be("InService");
    }

    [Fact]
    public async Task GetAll_ShouldReturnSuccessStatusCodeAndDeterministicList()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            context.Assets.RemoveRange(context.Assets); // Nettoyage pour isolation

            var asset = new Asset(assetId, "Switch Intégration", SerialNumber.Create("SWI-GET-ALL"), AssetType.NetworkDevice);
            await context.Assets.AddAsync(asset);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/assets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var assets = await response.Content.ReadFromJsonAsync<IEnumerable<AssetResponseDto>>();
        assets.Should().NotBeNull();
        assets.Should().ContainSingle(a => a.Id == assetId && a.Name == "Switch Intégration");
    }
}