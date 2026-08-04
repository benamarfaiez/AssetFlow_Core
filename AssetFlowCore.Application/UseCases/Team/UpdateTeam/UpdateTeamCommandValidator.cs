using AssetFlowCore.Domain.Enums;
using FluentValidation;

namespace AssetFlowCore.Application.UseCases.Team.UpdateTeam
{
    public class UpdateTeamCommandValidator : AbstractValidator<UpdateTeamCommand>
    {
        public UpdateTeamCommandValidator()
        {
            RuleFor(x => x.TeamId)
                .NotEmpty()
                .WithMessage("Le teamId est obligatoire.");

            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Le nom ne doit pas dépasser 100 caractères.");

            RuleFor(x => x.AssetType)
                .IsEnumName(typeof(AssetType), caseSensitive: false)
                .WithMessage("Le type d'asset doit être l'un des suivants : Server, Laptop ou NetworkDevice.");

            RuleFor(x => x.TicketCriticality)
                .IsEnumName(typeof(TicketCriticality), caseSensitive: false)
                .WithMessage("La criticité doit être l'une des suivantes : Low, Medium ou High.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("La description ne doit pas dépasser 500 caractères.");
        }
    }
}