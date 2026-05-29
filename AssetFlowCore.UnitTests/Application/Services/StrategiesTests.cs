using Xunit;
using FluentAssertions;
using AssetFlowCore.Application.Services;
using AssetFlowCore.Domain.Enums;

namespace AssetFlowCore.UnitTests.Application.Services;

public class StrategiesTests
{
    #region ServerAssignmentStrategy Tests

    [Fact]
    public void ServerAssignmentStrategy_GetTeam_ShouldReturnInfrastructureServeurs()
    {
        // Arrange
        var strategy = new ServerAssignmentStrategy();

        // Act
        var team = strategy.GetTeam();

        // Assert
        team.Should().Be("Infrastructure-Serveurs");
    }

    [Theory]
    [InlineData(TicketCriticality.Low, true)]
    [InlineData(TicketCriticality.Medium, true)]
    [InlineData(TicketCriticality.High, true)]
    public void ServerAssignmentStrategy_IsMatch_WhenAssetIsServer_ShouldReturnTrue(TicketCriticality criticality, bool expectedResult)
    {
        // Arrange
        var strategy = new ServerAssignmentStrategy();

        // Act
        var result = strategy.IsMatch(AssetType.Server, criticality);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(AssetType.Laptop)]
    [InlineData(AssetType.NetworkDevice)]
    public void ServerAssignmentStrategy_IsMatch_WhenAssetIsNotServer_ShouldReturnFalse(AssetType assetType)
    {
        // Arrange
        var strategy = new ServerAssignmentStrategy();

        // Act
        var result = strategy.IsMatch(assetType, TicketCriticality.High);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region NetworkAssignmentStrategy Tests

    [Fact]
    public void NetworkAssignmentStrategy_GetTeam_ShouldReturnReseauTelecom()
    {
        // Arrange
        var strategy = new NetworkAssignmentStrategy();

        // Act
        var team = strategy.GetTeam();

        // Assert
        team.Should().Be("Réseau-Télécom");
    }

    [Theory]
    [InlineData(TicketCriticality.Low, true)]
    [InlineData(TicketCriticality.Medium, true)]
    [InlineData(TicketCriticality.High, true)]
    public void NetworkAssignmentStrategy_IsMatch_WhenAssetIsNetworkDevice_ShouldReturnTrue(TicketCriticality criticality, bool expectedResult)
    {
        // Arrange
        var strategy = new NetworkAssignmentStrategy();

        // Act
        var result = strategy.IsMatch(AssetType.NetworkDevice, criticality);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(AssetType.Server)]
    [InlineData(AssetType.Laptop)]
    public void NetworkAssignmentStrategy_IsMatch_WhenAssetIsNotNetworkDevice_ShouldReturnFalse(AssetType assetType)
    {
        // Arrange
        var strategy = new NetworkAssignmentStrategy();

        // Act
        var result = strategy.IsMatch(assetType, TicketCriticality.High);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region LaptopHighCriticalityStrategy Tests

    [Fact]
    public void LaptopHighCriticalityStrategy_GetTeam_ShouldReturnSupportVIP()
    {
        // Arrange
        var strategy = new LaptopHighCriticalityStrategy();

        // Act
        var team = strategy.GetTeam();

        // Assert
        team.Should().Be("Support-VIP");
    }

    [Fact]
    public void LaptopHighCriticalityStrategy_IsMatch_WhenAssetIsLaptopAndCriticalityIsHigh_ShouldReturnTrue()
    {
        // Arrange
        var strategy = new LaptopHighCriticalityStrategy();

        // Act
        var result = strategy.IsMatch(AssetType.Laptop, TicketCriticality.High);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(AssetType.Laptop, TicketCriticality.Low)]
    [InlineData(AssetType.Laptop, TicketCriticality.Medium)]
    [InlineData(AssetType.Server, TicketCriticality.High)]
    [InlineData(AssetType.NetworkDevice, TicketCriticality.High)]
    public void LaptopHighCriticalityStrategy_IsMatch_WhenConditionsAreNotMet_ShouldReturnFalse(AssetType assetType, TicketCriticality criticality)
    {
        // Arrange
        var strategy = new LaptopHighCriticalityStrategy();

        // Act
        var result = strategy.IsMatch(assetType, criticality);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region LaptopStandardStrategy Tests

    [Fact]
    public void LaptopStandardStrategy_GetTeam_ShouldReturnSupportLectorat()
    {
        // Arrange
        var strategy = new LaptopStandardStrategy();

        // Act
        var team = strategy.GetTeam();

        // Assert
        team.Should().Be("Support-Lectorat");
    }

    [Theory]
    [InlineData(TicketCriticality.Low)]
    [InlineData(TicketCriticality.Medium)]
    public void LaptopStandardStrategy_IsMatch_WhenAssetIsLaptopAndCriticalityIsNotHigh_ShouldReturnTrue(TicketCriticality criticality)
    {
        // Arrange
        var strategy = new LaptopStandardStrategy();

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
        var strategy = new LaptopStandardStrategy();

        // Act
        var result = strategy.IsMatch(assetType, criticality);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}