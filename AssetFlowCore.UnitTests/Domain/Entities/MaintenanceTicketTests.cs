using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using FluentAssertions;

namespace AssetFlowCore.UnitTests.Domain.Entities;

public class MaintenanceTicketTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var assetId = Guid.NewGuid();
        var ticket = new MaintenanceTicket(Guid.NewGuid(), assetId, "Titre", "Description", TicketCriticality.High, "Equipe-A");

        ticket.AssetId.Should().Be(assetId);
        ticket.Status.Should().Be(TicketStatus.Opened);
        ticket.ResolutionComment.Should().BeNull();
    }

    [Theory]
    [InlineData("", "Desc", "Team")]
    [InlineData("Titre", " ", "Team")]
    [InlineData("Titre", "Desc", null)]
    public void Constructor_WithMissingParameters_ShouldThrowArgumentException(string? title, string? desc, string? team)
    {
        Action act = () => new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), title!, desc!, TicketCriticality.Low, team!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignToTechnician_WhenOpened_ShouldTransitionToInProgress()
    {
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Desc", TicketCriticality.Medium, "Team");
        ticket.AssignToTechnician();
        ticket.Status.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public void AssignToTechnician_WhenAlreadyProcessed_ShouldThrowInvalidOperationException()
    {
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Desc", TicketCriticality.Medium, "Team");
        ticket.AssignToTechnician(); // InProgress

        Action act = () => ticket.AssignToTechnician();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Seul un ticket ouvert peut être pris en charge*");
    }

    [Fact]
    public void Close_WithValidComment_ShouldTransitionToClosed()
    {
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Desc", TicketCriticality.Medium, "Team");
        ticket.AssignToTechnician();

        ticket.Close("Résolu avec succès");

        ticket.Status.Should().Be(TicketStatus.Closed);
        ticket.ResolutionComment.Should().Be("Résolu avec succès");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Close_WithInvalidComment_ShouldThrowArgumentException(string? invalidComment)
    {
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Desc", TicketCriticality.Medium, "Team");
        ticket.AssignToTechnician();

        Action act = () => ticket.Close(invalidComment!);
        act.Should().Throw<ArgumentException>().WithMessage("*Un commentaire de résolution est obligatoire*");
    }
}