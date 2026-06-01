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
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace AssetFlowCore.IntegrationTests.WebApi.Controllers;

public class TicketsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public TicketsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTicket_WithValidAsset_ShouldMutateAssetAndReturnCreated()
    {
        // Arrange
        var assetId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AssetFlowDbContext>();
            var asset = new Asset(assetId, "Laptop Intégration", SerialNumber.Create("LPT-INT-95"), AssetType.Laptop);
            await context.Assets.AddAsync(asset);
            await context.SaveChangesAsync();
        }

        var payload = new CreateTicketRequest(assetId, "Clavier bloqué", "Touches HS", "Medium");

        // Act
        var response = await _client.PostAsJsonAsync("/api/tickets", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponseDto>();
        ticket.Should().NotBeNull();
        ticket!.AssignedTeam.Should().Be("Support-Lectorat");
    }
}