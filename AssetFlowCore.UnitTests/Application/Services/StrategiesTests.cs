using AssetFlowCore.Application.Services;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace AssetFlowCore.UnitTests.Application.Services;

public class StrategiesTests
{
    private readonly Mock<ITeamRepository> _teamRepository = new();

    #region ServerAssignmentStrategy Tests

    [Fact]
    public async Task ServerAssignmentStrategy_GetTeam_ShouldReturnInfrastructureServeurs()
    {
        // Arrange
        var strategy = new ServerAssignmentStrategy(_teamRepository.Object);
        var assetType = AssetType.Server.ToString();
        var criticality = TicketCriticality.Medium;
        var team = new Team("ServerAssignment", assetType, criticality.ToString(), "description");
        _teamRepository.Setup(r => r.GetByAssetTypeAndCriticalityAsync(assetType, criticality.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(team);

        // Act
        var teamName = await strategy.GetTeamNameAsync(AssetType.Server.ToString(), criticality.ToString());

        // Assert
        teamName.Should().Be(team.Name);
    }

    [Theory]
    [InlineData(TicketCriticality.Low, true)]
    [InlineData(TicketCriticality.Medium, true)]
    [InlineData(TicketCriticality.High, true)]
    public void ServerAssignmentStrategy_IsMatch_WhenAssetIsServer_ShouldReturnTrue(TicketCriticality criticality, bool expectedResult)
    {
        // Arrange
        var strategy = new ServerAssignmentStrategy(_teamRepository.Object);
        var assetType = AssetType.Server;
        var team = new Team("ServerAssignment", assetType.ToString(), criticality.ToString(), "description");
        _teamRepository.Setup(r => r.GetByAssetTypeAndCriticalityAsync(assetType.ToString(), criticality.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(team);

        // Act
        var result = strategy.IsMatch(assetType, criticality);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(AssetType.Laptop)]
    [InlineData(AssetType.NetworkDevice)]
    public void ServerAssignmentStrategy_IsMatch_WhenAssetIsNotServer_ShouldReturnFalse(AssetType assetType)
    {
        // Arrange
        var strategy = new ServerAssignmentStrategy(_teamRepository.Object);
        var criticality = TicketCriticality.Medium;
        var team = new Team("ServerAssignment", assetType.ToString(), criticality.ToString(), "description");
        _teamRepository.Setup(r => r.GetByAssetTypeAndCriticalityAsync(assetType.ToString(), criticality.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(team);

        // Act
        var result = strategy.IsMatch(assetType, TicketCriticality.High);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region NetworkAssignmentStrategy Tests

    [Fact]
    public async Task NetworkAssignmentStrategy_GetTeam_ShouldReturnReseauTelecom()
    {
        // Arrange
        var strategy = new NetworkAssignmentStrategy(_teamRepository.Object);
        var assetType = AssetType.NetworkDevice.ToString();
        var criticality = TicketCriticality.Medium;
        var team = new Team("ServerAssignment", assetType, criticality.ToString(), "description");

        _teamRepository
            .Setup(r => r.GetByAssetTypeAndCriticalityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Act
        var teamName = await strategy.GetTeamNameAsync(AssetType.Server.ToString(), criticality.ToString());

        // Assert
        teamName.Should().Be(team.Name);
    }

    [Theory]
    [InlineData(TicketCriticality.Low, true)]
    [InlineData(TicketCriticality.Medium, true)]
    [InlineData(TicketCriticality.High, true)]
    public void NetworkAssignmentStrategy_IsMatch_WhenAssetIsNetworkDevice_ShouldReturnTrue(TicketCriticality criticality, bool expectedResult)
    {
        // Arrange
        var strategy = new NetworkAssignmentStrategy(_teamRepository.Object);
        var assetType = AssetType.NetworkDevice;

        // Act
        var result = strategy.IsMatch(assetType, criticality);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(AssetType.Server)]
    [InlineData(AssetType.Laptop)]
    public void NetworkAssignmentStrategy_IsMatch_WhenAssetIsNotNetworkDevice_ShouldReturnFalse(AssetType assetType)
    {
        // Arrange
        var strategy = new NetworkAssignmentStrategy(_teamRepository.Object);
        var criticality = TicketCriticality.Medium;
        var team = new Team("ServerAssignment", assetType.ToString(), criticality.ToString(), "description");
        _teamRepository.Setup(r => r.GetByAssetTypeAndCriticalityAsync(assetType.ToString(), criticality.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(team);

        // Act
        var result = strategy.IsMatch(assetType, TicketCriticality.High);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region LaptopHighCriticalityStrategy Tests

    [Fact]
    public async Task LaptopHighCriticalityStrategy_GetTeam_ShouldReturnSupportVIP()
    {
        // Arrange
        var strategy = new LaptopHighCriticalityStrategy(_teamRepository.Object);
        var assetType = AssetType.Laptop.ToString();
        var criticality = TicketCriticality.High;
        var team = new Team("LaptopHighCriticality", assetType, criticality.ToString(), "description");
        _teamRepository
            .Setup(r => r.GetByAssetTypeAndCriticalityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        // Act
        var teamName = await strategy.GetTeamNameAsync(AssetType.Server.ToString(), criticality.ToString());

        // Assert
        teamName.Should().Be(team.Name);
    }

    [Theory]
    [InlineData(AssetType.Laptop, TicketCriticality.Low)]
    [InlineData(AssetType.Laptop, TicketCriticality.Medium)]
    [InlineData(AssetType.Server, TicketCriticality.High)]
    [InlineData(AssetType.NetworkDevice, TicketCriticality.High)]
    public void LaptopHighCriticalityStrategy_IsMatch_WhenConditionsAreNotMet_ShouldReturnFalse(AssetType assetType, TicketCriticality criticality)
    {
        // Arrange
        var strategy = new LaptopHighCriticalityStrategy(_teamRepository.Object);

        // Act
        var result = strategy.IsMatch(assetType, criticality);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region LaptopStandardStrategy Tests

    [Fact]
    public async Task LaptopStandardStrategy_GetTeam_ShouldReturnSupportLectorat()
    {
        // Arrange
        var strategy = new LaptopStandardStrategy(_teamRepository.Object);
        var assetType = AssetType.Server;
        var criticality = TicketCriticality.Low;
        var team = new Team("ServerAssignment", assetType.ToString(), criticality.ToString(), "description");
        _teamRepository.Setup(r => r.GetByAssetTypeAndCriticalityAsync(assetType.ToString(), criticality.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(team);

        // Act
        var result = await strategy.GetTeamNameAsync(assetType.ToString(), criticality.ToString());

        // Assert
        result.Should().Be(team.Name);
    }

    [Theory]
    [InlineData(TicketCriticality.Low)]
    [InlineData(TicketCriticality.Medium)]
    public void LaptopStandardStrategy_IsMatch_WhenAssetIsLaptopAndCriticalityIsNotHigh_ShouldReturnTrue(TicketCriticality criticality)
    {
        // Arrange
        var strategy = new LaptopStandardStrategy(_teamRepository.Object);
        var assetType = AssetType.Server;

        var team = new Team("ServerAssignment", assetType.ToString(), criticality.ToString(), "description");
        _teamRepository.Setup(r => r.GetByAssetTypeAndCriticalityAsync(assetType.ToString(), criticality.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(team);
        // Act
        var result = strategy.IsMatch(AssetType.Laptop, criticality);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(AssetType.Laptop, TicketCriticality.High)] // Condition d'exclusion (VIP)
    [InlineData(AssetType.Server, TicketCriticality.Low)]
    [InlineData(AssetType.NetworkDevice, TicketCriticality.Medium)]
    public void LaptopStandardStrategy_IsMatch_WhenConditionsAreNotMet_ShouldReturnFalse(AssetType assetType, TicketCriticality criticality)
    {
        // Arrange
        var strategy = new LaptopStandardStrategy(_teamRepository.Object);

        var team = new Team("ServerAssignment", assetType.ToString(), criticality.ToString(), "description");
        _teamRepository.Setup(r => r.GetByAssetTypeAndCriticalityAsync(assetType.ToString(), criticality.ToString(), It.IsAny<CancellationToken>())).ReturnsAsync(team);

        // Act
        var result = strategy.IsMatch(assetType, criticality);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
