using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Application.Services;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace AssetFlowCore.UnitTests.Application.Services;

public class TicketAssignmentEngineTests
{
    private readonly TicketAssignmentEngine _engine;
    private readonly Mock<ITeamRepository> _teamRepository = new();

    public TicketAssignmentEngineTests()
    {
        _teamRepository = new Mock<ITeamRepository>();
        _teamRepository
            .Setup(r => r.GetByAssetTypeAndCriticalityAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string type, string crit) =>
            {
                // On simule le contenu de la base de données selon les entrées
                return (type, crit) switch
                {
                    ("Server", "Low") => new Team("Infrastructure-Serveurs", type, crit, "desc"),
                    ("NetworkDevice", "High") => new Team("Réseau-Télécom", type, crit, "desc"),
                    ("Laptop", "High") => new Team("Support-VIP", type, crit, "desc"),
                    ("Laptop", "Medium") => new Team("Support-Lectorat", type, crit, "desc"),
                    _ => new Team("Team-Fallback", "Laptop", "Low", "desc")
                };
            });

        var strategies = new List<IAssignmentStrategy>
        {
            new ServerAssignmentStrategy(_teamRepository.Object),
            new NetworkAssignmentStrategy(_teamRepository.Object),
            new LaptopHighCriticalityStrategy(_teamRepository.Object),
            new LaptopStandardStrategy(_teamRepository.Object)
        };
        _engine = new TicketAssignmentEngine(strategies);
    }

    [Theory]
    [InlineData(AssetType.Server, TicketCriticality.Low, "Infrastructure-Serveurs")]
    [InlineData(AssetType.NetworkDevice, TicketCriticality.High, "Réseau-Télécom")]
    [InlineData(AssetType.Laptop, TicketCriticality.High, "Support-VIP")]
    [InlineData(AssetType.Laptop, TicketCriticality.Medium, "Support-Lectorat")]
    public async Task ResolveTeam_ShouldReturnExpectedTeam(AssetType assetType, TicketCriticality criticality, string expectedTeam)
    {
        var team = await _engine.ResolveTeamIdAsync(assetType, criticality);
        team.Should().Be(expectedTeam);
    }

    [Fact]
    public async Task ResolveTeam_WhenNoStrategiesMatch_ShouldReturnFallbackTeam()
    {
        // Arrange
        var assetType = AssetType.Server;
        var criticality = TicketCriticality.Low;

        // 1. On prépare l'équipe que le repository doit retourner
        var team = new Team("Team A", assetType.ToString(), criticality.ToString(), "Description");
        _teamRepository.Setup(
            r => r.GetByAssetTypeAndCriticalityAsync(assetType.ToString(), criticality.ToString())
        ).ReturnsAsync(team);

        // 2. On instancie la stratégie de fallback attendue par le moteur
        var fallbackStrategy = new LaptopStandardStrategy(_teamRepository.Object);

        // 3. On injecte cette stratégie dans le moteur. 
        // Elle ne matchera pas (car c'est un Server), mais elle sera disponible pour le .First()
        var engine = new TicketAssignmentEngine([fallbackStrategy]);

        // Act
        string result = await engine.ResolveTeamIdAsync(assetType, criticality);

        // Assert
        result.Should().Be(team.Name);
    }
}