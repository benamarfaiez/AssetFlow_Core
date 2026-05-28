using FluentValidation;
using AssetFlowCore.Domain.Enums;
using System;

namespace AssetFlowCore.Application.UseCases.Tickets.CreateTicket;

/// <summary>
/// Validateur de surface pour la commande de création de ticket.
/// Garantit la conformité syntaxique des données entrantes.
/// </summary>
public class CreateMaintenanceTicketValidator : AbstractValidator<CreateMaintenanceTicketCommand>
{
    public CreateMaintenanceTicketValidator()
    {
        // 1. Validation de l'identifiant de l'actif
        RuleFor(command => command.AssetId)
            .NotEmpty()
            .WithMessage("L'identifiant de l'actif cible (AssetId) est obligatoire.");

        // 2. Validation du titre de l'incident
        RuleFor(command => command.Title)
            .NotEmpty()
            .WithMessage("Le titre du ticket est obligatoire.")
            .MaximumLength(150)
            .WithMessage("Le titre du ticket ne doit pas dépasser 150 caractères.");

        // 3. Validation de la description
        RuleFor(command => command.Description)
            .NotEmpty()
            .WithMessage("La description détaillée de l'anomalie est obligatoire.");

        // 4. Validation de la criticité par rapport à l'énumération du Domaine
        RuleFor(command => command.Criticality)
            .NotEmpty()
            .WithMessage("Le niveau de criticité est obligatoire.")
            .IsEnumName(typeof(TicketCriticality), caseSensitive: false)
            .WithMessage("La criticité fournie n'est pas valide. Valeurs autorisées : Low, Medium, High.");
    }
}