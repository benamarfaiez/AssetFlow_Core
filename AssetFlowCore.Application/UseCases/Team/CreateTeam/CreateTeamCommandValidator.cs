using FluentValidation;

namespace AssetFlowCore.Application.UseCases.Team.CreateTeam;

public class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom de l'équipe est obligatoire.")
            .MaximumLength(100).WithMessage("Le nom ne doit pas dépasser 100 caractères.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La description ne doit pas dépasser 500 caractères.");
    }
}