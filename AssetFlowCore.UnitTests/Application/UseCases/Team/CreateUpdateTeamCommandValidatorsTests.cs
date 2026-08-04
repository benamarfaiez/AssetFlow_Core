using AssetFlowCore.Application.UseCases.Team.CreateTeam;
using AssetFlowCore.Application.UseCases.Team.UpdateTeam;
using AssetFlowCore.Domain.Enums;
using FluentValidation.TestHelper;

namespace AssetFlowCore.UnitTests.Application.UseCases.Team
{
    public class CreateUpdateTeamCommandValidatorsTests
    {
        [Fact]
        public void CreateValidator_Should_HaveErrors_ForInvalidInputs()
        {
            var validator = new CreateTeamCommandValidator();

            var invalid = new CreateTeamCommand("", "Unknown", "Bad", new string('x', 600));
            var result = validator.TestValidate(invalid);

            result.ShouldHaveValidationErrorFor(x => x.Name);
            result.ShouldHaveValidationErrorFor(x => x.AssetType);
            result.ShouldHaveValidationErrorFor(x => x.TicketCriticality);
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void CreateValidator_Should_Pass_ForValidInputs()
        {
            var validator = new CreateTeamCommandValidator();
            var valid = new CreateTeamCommand("Team X", AssetType.Server.ToString(), TicketCriticality.High.ToString(), "desc");

            var result = validator.TestValidate(valid);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void UpdateValidator_Should_HaveErrors_ForInvalidInputs()
        {
            var validator = new UpdateTeamCommandValidator();
            var invalid = new UpdateTeamCommand(Guid.Empty, new string('x', 200), "BadType", "BadCrit", new string('y', 600));

            var result = validator.TestValidate(invalid);
            result.ShouldHaveValidationErrorFor(x => x.TeamId);
            result.ShouldHaveValidationErrorFor(x => x.Name);
            result.ShouldHaveValidationErrorFor(x => x.AssetType);
            result.ShouldHaveValidationErrorFor(x => x.TicketCriticality);
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void UpdateValidator_Should_Pass_ForValidInputs()
        {
            var validator = new UpdateTeamCommandValidator();
            var valid = new UpdateTeamCommand(Guid.NewGuid(), "Name", AssetType.Laptop.ToString(), TicketCriticality.Low.ToString(), "desc");

            var result = validator.TestValidate(valid);
            result.ShouldNotHaveAnyValidationErrors();
        }

        // Correction 1.5 : le message de criticité était recopié depuis le type d'actif
        // et annonçait « Server, Laptop ou NetworkDevice » sur un champ de criticité.

        [Fact]
        public void CreateValidator_Should_ReportCriticalityValues_OnInvalidCriticality()
        {
            var validator = new CreateTeamCommandValidator();
            var invalid = new CreateTeamCommand("Team X", AssetType.Server.ToString(), "Urgent", "desc");

            var result = validator.TestValidate(invalid);

            result.ShouldHaveValidationErrorFor(x => x.TicketCriticality)
                  .WithErrorMessage("La criticité doit être l'une des suivantes : Low, Medium ou High.");
        }

        [Fact]
        public void CreateValidator_Should_ReportCriticalityRequired_OnEmptyCriticality()
        {
            var validator = new CreateTeamCommandValidator();
            var invalid = new CreateTeamCommand("Team X", AssetType.Server.ToString(), "", "desc");

            var result = validator.TestValidate(invalid);

            result.ShouldHaveValidationErrorFor(x => x.TicketCriticality)
                  .WithErrorMessage("La criticité prise en charge par l'équipe est obligatoire.");
        }

        [Fact]
        public void UpdateValidator_Should_ReportCriticalityValues_OnInvalidCriticality()
        {
            var validator = new UpdateTeamCommandValidator();
            var invalid = new UpdateTeamCommand(Guid.NewGuid(), "Name", AssetType.Laptop.ToString(), "Urgent", "desc");

            var result = validator.TestValidate(invalid);

            result.ShouldHaveValidationErrorFor(x => x.TicketCriticality)
                  .WithErrorMessage("La criticité doit être l'une des suivantes : Low, Medium ou High.");
        }
    }
}
