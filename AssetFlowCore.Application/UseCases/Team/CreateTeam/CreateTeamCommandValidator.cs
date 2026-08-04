using AssetFlowCore.Domain.Enums;
using FluentValidation;

namespace AssetFlowCore.Application.UseCases.Team.CreateTeam;

public class CreateTeamCommandValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Le nom de l'équipe est obligatoire.")
            .MaximumLength(100).WithMessage("Le nom ne doit pas dépasser 100 caractères.");

        RuleFor(x => x.AssetType)
            .NotEmpty().WithMessage("Le type d'asset est obligatoire.")
            .IsEnumName(typeof(AssetType), caseSensitive: false)
            .WithMessage("Le type d'asset doit être l'un des suivants : Server, Laptop ou NetworkDevice.");

        RuleFor(x => x.TicketCriticality)
            .NotEmpty().WithMessage("La criticité prise en charge par l'équipe est obligatoire.")
            .IsEnumName(typeof(TicketCriticality), caseSensitive: false)
            .WithMessage("La criticité doit être l'une des suivantes : Low, Medium ou High.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La description ne doit pas dépasser 500 caractères.");
    }
}