using Xunit;
using FluentAssertions;
using AssetFlowCore.Application.Services;
using AssetFlowCore.Domain.Enums;
using System.Collections.Generic;
using AssetFlowCore.Application.Interfaces;

namespace AssetFlowCore.UnitTests.Application.Services;

public class TicketAssignmentEngineTests
{
    private readonly TicketAssignmentEngine _engine;

    public TicketAssignmentEngineTests()
    {
        var strategies = new List<IAssignmentStrategy>
        {
            new ServerAssignmentStrategy(),
            new NetworkAssignmentStrategy(),
            new LaptopHighCriticalityStrategy(),
            new LaptopStandardStrategy()
        };
        _engine = new TicketAssignmentEngine(strategies);
    }

    [Theory]
    [InlineData(AssetType.Server, TicketCriticality.Low, "Infrastructure-Serveurs")]
    [InlineData(AssetType.NetworkDevice, TicketCriticality.High, "Réseau-Télécom")]
    [InlineData(AssetType.Laptop, TicketCriticality.High, "Support-VIP")]
    [InlineData(AssetType.Laptop, TicketCriticality.Medium, "Support-Lectorat")]
    public void ResolveTeam_ShouldReturnExpectedTeam(AssetType assetType, TicketCriticality criticality, string expectedTeam)
    {
        string team = _engine.ResolveTeam(assetType, criticality);
        team.Should().Be(expectedTeam);
    }

    [Fact]
    public void ResolveTeam_WhenNoStrategiesMatch_ShouldReturnFallbackTeam()
    {
        // Engine vide sans stratégie injectée
        var emptyEngine = new TicketAssignmentEngine(new List<IAssignmentStrategy>());
        string team = emptyEngine.ResolveTeam(AssetType.Server, TicketCriticality.Low);
        team.Should().Be("Support-Général");
    }
}