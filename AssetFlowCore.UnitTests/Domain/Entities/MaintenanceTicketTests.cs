using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using FluentAssertions;

namespace AssetFlowCore.UnitTests.Domain.Entities;

public class MaintenanceTicketTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        var assetId = Guid.NewGuid();
        var ticket = new MaintenanceTicket(Guid.NewGuid(), assetId, "Titre", "Description", TicketCriticality.High, Guid.NewGuid());

        ticket.AssetId.Should().Be(assetId);
        ticket.Status.Should().Be(TicketStatus.Opened);
        ticket.ResolutionComment.Should().BeNull();
    }

    [Theory]
    [InlineData("", "Desc")]
    [InlineData("Titre", " ")]
    public void Constructor_WithMissingParameters_ShouldThrowArgumentException(string? title, string? desc)
    {
        Action act = () =>
        {
            MaintenanceTicket maintenanceTicket = new(Guid.NewGuid(), Guid.NewGuid(), title!, desc!, TicketCriticality.Low, Guid.NewGuid());
        };
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AssignToTechnician_WhenOpened_ShouldTransitionToInProgress()
    {
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Desc", TicketCriticality.Medium, Guid.NewGuid());
        ticket.AssignToTechnician();
        ticket.Status.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public void AssignToTechnician_WhenAlreadyProcessed_ShouldThrowDomainException()
    {
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Desc", TicketCriticality.Medium, Guid.NewGuid());
        ticket.AssignToTechnician(); // InProgress

        Action act = () => ticket.AssignToTechnician();
        act.Should().Throw<DomainException>().WithMessage("*Seul un ticket ouvert peut être pris en charge*");
    }

    [Fact]
    public void Close_WithValidComment_ShouldTransitionToClosed()
    {
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Desc", TicketCriticality.Medium, Guid.NewGuid());
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
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Desc", TicketCriticality.Medium, Guid.NewGuid());
        ticket.AssignToTechnician();

        Action act = () => ticket.Close(invalidComment!);
        act.Should().Throw<ArgumentException>().WithMessage("*Un commentaire de résolution est obligatoire*");
    }

    [Fact]
    public void Close_WithInvalidStatus_ShouldThrowDomainException()
    {
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Desc", TicketCriticality.Medium, Guid.NewGuid());

        Action act = () => ticket.Close("Résolu");
        act.Should().Throw<DomainException>().WithMessage("Seul un ticket en cours peut être clôturé.");
    }

    [Fact]
    public void TransferToTeam_ShouldThrowDomainException()
    {
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Desc", TicketCriticality.Medium, Guid.NewGuid());
        ticket.AssignToTechnician();
        ticket.Close("Résolu");
        var newTeam = new Team("name", "asset", "Low", "desc");

        Action act = () => ticket.TransferToTeam(newTeam, "Reason");
        act.Should().Throw<DomainException>().WithMessage("Impossible de transférer un ticket clôturé.");
    }

    [Fact]
    public void TransferToTeam_Should_UpdateAssignedTeam_When_RuleIsValid()
    {
        // Arrange
        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Description", TicketCriticality.High, Guid.NewGuid());
        var newTeam = new Team("name", "asset", "Low", "desc");

        // Act
        ticket.TransferToTeam(newTeam, "Besoin d'une expertise réseau.");

        // Assert
        ticket.AssignedTeam.Should().Be(newTeam);
    }

    [Fact]
    public void TransferToTeam_Should_ThrowDomainException_When_TargetTeamIsSame()
    {
        // Arrange
        var team = new Team("name", "asset", "Low", "desc");

        var ticket = new MaintenanceTicket(Guid.NewGuid(), Guid.NewGuid(), "Titre", "Description", TicketCriticality.High, team.Id);

        // Act
        var action = () => ticket.TransferToTeam(team, "Motif");

        // Assert
        action.Should().Throw<DomainException>()
              .WithMessage($"Le ticket est déjà assigné à l'équipe '{team.Name}'.");
    }
}