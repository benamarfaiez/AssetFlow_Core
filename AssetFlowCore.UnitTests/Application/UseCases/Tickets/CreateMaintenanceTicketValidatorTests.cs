using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using AssetFlowCore.Domain.Enums;
using FluentValidation.TestHelper;

namespace AssetFlowCore.UnitTests.Application.UseCases.Tickets;

/// <summary>
/// Couvre la correction 1.6 : la règle sur la criticité ne contrôlait que la présence de la
/// valeur, malgré un message annonçant une liste fermée. Une criticité inconnue traversait donc
/// la validation et échouait plus loin sur la conversion d'énumération.
/// </summary>
public class CreateMaintenanceTicketValidatorTests
{
    private readonly CreateMaintenanceTicketValidator _validator = new();

    [Theory]
    [InlineData("Urgent")]
    [InlineData("Critical")]
    [InlineData("ULTRA_CRITIQUE")]
    public void Validate_WithUnknownCriticality_ShouldReportErrorOnCriticalityField(string criticality)
    {
        var command = new CreateMaintenanceTicketCommand(
            Guid.NewGuid(), "Panne du disque principal", "Le serveur ne démarre plus.", criticality);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Criticality)
              .WithErrorMessage("La criticité fournie n'est pas valide. Valeurs autorisées : Low, Medium, High.");
    }

    [Theory]
    [InlineData("Low")]
    [InlineData("medium")]
    [InlineData("HIGH")]
    public void Validate_WithKnownCriticality_ShouldPass(string criticality)
    {
        var command = new CreateMaintenanceTicketCommand(
            Guid.NewGuid(), "Panne du disque principal", "Le serveur ne démarre plus.", criticality);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Criticality);
    }

    [Fact]
    public void Validate_WithEmptyCriticality_ShouldReportRequiredMessage()
    {
        var command = new CreateMaintenanceTicketCommand(
            Guid.NewGuid(), "Panne du disque principal", "Le serveur ne démarre plus.", string.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Criticality)
              .WithErrorMessage("Le niveau de criticité est obligatoire.");
    }

    [Fact]
    public void Validate_WithEveryDomainCriticality_ShouldPass()
    {
        foreach (var criticality in Enum.GetNames<TicketCriticality>())
        {
            var command = new CreateMaintenanceTicketCommand(
                Guid.NewGuid(), "Titre", "Description", criticality);

            _validator.TestValidate(command).ShouldNotHaveValidationErrorFor(x => x.Criticality);
        }
    }
}
