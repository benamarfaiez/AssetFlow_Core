using AssetFlowCore.Domain.Entities;
using FluentAssertions;

namespace AssetFlowCore.UnitTests.Domain.Entities;

public class TeamTests
{
    // ──────────────────────────────────────────────────────────────────────
    // Constructor – happy path
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var team = new Team("Infrastructure-Serveurs", "Server", "High", "Équipe serveurs");

        team.Id.Should().NotBeEmpty();
        team.Name.Should().Be("Infrastructure-Serveurs");
        team.AssetType.Should().Be("Server");
        team.TicketCriticality.Should().Be("High");
        team.Description.Should().Be("Équipe serveurs");
        team.IsActive.Should().BeTrue();
        team.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        team.Tickets.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullDescription_ShouldSetDescriptionToNull()
    {
        var team = new Team("Support-VIP", "Laptop", "Low");

        team.Description.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldTrimWhitespace_ForStringProperties()
    {
        var team = new Team("  Réseau  ", "  Network  ", "  Medium  ", "  Infra réseau  ");

        team.Name.Should().Be("Réseau");
        team.AssetType.Should().Be("Network");
        team.TicketCriticality.Should().Be("Medium");
        team.Description.Should().Be("Infra réseau");
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds_ForEachInstance()
    {
        var team1 = new Team("Équipe-A", "Server", "High");
        var team2 = new Team("Équipe-B", "Laptop", "Low");

        team1.Id.Should().NotBe(team2.Id);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Constructor – Name validation
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        Action act = () => new Team(invalidName!, "Server", "High");

        act.Should().Throw<ArgumentException>()
           .WithParameterName("name")
           .WithMessage("*Le nom de l'équipe est obligatoire*");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Constructor – AssetType validation
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidAssetType_ShouldThrowArgumentException(string? invalidAssetType)
    {
        Action act = () => new Team("Équipe-X", invalidAssetType!, "High");

        act.Should().Throw<ArgumentException>()
           .WithParameterName("assetType")
           .WithMessage("*Le assetType de l'équipe est obligatoire*");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Constructor – TicketCriticality validation
    // ──────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidTicketCriticality_ShouldThrowArgumentException(string? invalidCriticality)
    {
        Action act = () => new Team("Équipe-X", "Server", invalidCriticality!);

        act.Should().Throw<ArgumentException>()
           .WithParameterName("ticketCriticality")
           .WithMessage("*Le ticketCriticality de l'équipe est obligatoire*");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Deactivate / Activate
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_WhenActive_ShouldSetIsActiveToFalse()
    {
        var team = new Team("Équipe-Ops", "Server", "High");

        team.Deactivate();

        team.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_WhenInactive_ShouldSetIsActiveToTrue()
    {
        var team = new Team("Équipe-Ops", "Server", "High");
        team.Deactivate();

        team.Activate();

        team.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldRemainActive()
    {
        var team = new Team("Équipe-Ops", "Server", "High");

        team.Activate(); // called on already-active team

        team.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ShouldRemainInactive()
    {
        var team = new Team("Équipe-Ops", "Server", "High");
        team.Deactivate();

        team.Deactivate(); // idempotent call

        team.IsActive.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────
    // UpdateDescription
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateDescription_WithValidValue_ShouldUpdateDescription()
    {
        var team = new Team("Équipe-Dev", "Laptop", "Medium", "Ancienne description");

        team.UpdateDescription("Nouvelle description");

        team.Description.Should().Be("Nouvelle description");
    }

    [Fact]
    public void UpdateDescription_ShouldTrimWhitespace()
    {
        var team = new Team("Équipe-Dev", "Laptop", "Medium");

        team.UpdateDescription("  Description avec espaces  ");

        team.Description.Should().Be("Description avec espaces");
    }

    [Fact]
    public void UpdateDescription_WithNull_ShouldSetDescriptionToNull()
    {
        var team = new Team("Équipe-Dev", "Laptop", "Medium", "Ancienne description");

        team.UpdateDescription(null!);

        team.Description.Should().BeNull();
    }
}
