using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.ValueObjects;
using FluentAssertions;

namespace AssetFlowCore.UnitTests.Domain.Entities;

public class AssetTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var id = Guid.NewGuid();
        var serial = SerialNumber.Create("SRV-12345");

        var asset = new Asset(id, "Serveur-01", serial, AssetType.Server);

        asset.Id.Should().Be(id);
        asset.Name.Should().Be("Serveur-01");
        asset.SerialNumber.Should().Be(serial);
        asset.Type.Should().Be(AssetType.Server);
        asset.Status.Should().Be(AssetStatus.InService);
        asset.Tickets.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        Action act = () => new Asset(Guid.NewGuid(), invalidName!, SerialNumber.Create("SRV-12345"), AssetType.Server);
        act.Should().Throw<ArgumentException>().WithMessage("*Le nom de l'actif ne peut pas être vide*");
    }

    [Fact]
    public void MarkAsDown_WhenInService_ShouldTransitionToDown()
    {
        var asset = new Asset(Guid.NewGuid(), "PC-Prod", SerialNumber.Create("LAP-12345"), AssetType.Laptop);
        asset.MarkAsDown();
        asset.Status.Should().Be(AssetStatus.Down);
    }

    [Fact]
    public void MarkAsDown_WhenDecommissioned_ShouldThrowDomainException()
    {
        var asset = new Asset(Guid.NewGuid(), "PC-Prod", SerialNumber.Create("LAP-12345"), AssetType.Laptop);
        asset.Decommission();

        Action act = () => asset.MarkAsDown();
        act.Should().Throw<DomainException>().WithMessage("*Impossible de mettre en panne un actif mis au rebut*");
    }

    [Fact]
    public void MarkInMaintenance_WhenDown_ShouldTransitionToInMaintenance()
    {
        var asset = new Asset(Guid.NewGuid(), "PC-Prod", SerialNumber.Create("LAP-12345"), AssetType.Laptop);
        asset.MarkAsDown();

        asset.MarkInMaintenance();
        asset.Status.Should().Be(AssetStatus.InMaintenance);
    }

    [Fact]
    public void MarkInMaintenance_WhenNotInServiceOrDown_ShouldThrowDomainException()
    {
        var asset = new Asset(Guid.NewGuid(), "PC-Prod", SerialNumber.Create("LAP-12345"), AssetType.Laptop);

        Action act = () => asset.MarkInMaintenance();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RestoreToService_ShouldSetStatusToInService()
    {
        var asset = new Asset(Guid.NewGuid(), "PC-Prod", SerialNumber.Create("LAP-12345"), AssetType.Laptop);
        asset.MarkAsDown();

        asset.RestoreToService();
        asset.Status.Should().Be(AssetStatus.InService);
    }
}