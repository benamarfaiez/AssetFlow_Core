using AssetFlowCore.Application.UseCases.Tickets.TransferTicket;
using FluentValidation.TestHelper;

namespace AssetFlowCore.UnitTests.Application.UseCases.Tickets;

public class RequestTicketTransferCommandValidatorTests
{
    private readonly RequestTicketTransferCommandValidator _validator;

    public RequestTicketTransferCommandValidatorTests()
    {
        _validator = new RequestTicketTransferCommandValidator();
    }

    [Fact]
    public void Should_Pass_Validation_When_Command_Is_Valid()
    {
        // Arrange
        var command = new RequestTicketTransferCommand(
            Guid.NewGuid(),
            "Équipe Support IT",
            "Une description ou un autre paramètre requis"
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_TicketId_Is_Empty()
    {
        // Arrange
        var command = new RequestTicketTransferCommand
        (
            Guid.Empty,
            "Équipe Support IT",
            "Transfert nécessaire pour expertise spécialisée."
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TicketId)
              .WithErrorMessage("L'identifiant du ticket est requis.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_Have_Error_When_TeamName_Is_Invalid(string invalidTeamName)
    {
        // Arrange
        var command = new RequestTicketTransferCommand
        (
            Guid.NewGuid(),
            invalidTeamName,
            "Transfert nécessaire pour expertise spécialisée."
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TeamName)
              .WithErrorMessage("L'équipe cible est requise.");
    }
}
