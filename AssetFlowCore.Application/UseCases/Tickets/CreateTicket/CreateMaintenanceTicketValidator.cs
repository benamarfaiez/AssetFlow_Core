using FluentValidation;

namespace AssetFlowCore.Application.UseCases.Tickets.CreateTicket;

/// <summary>
/// Validateur de surface pour la commande de création de ticket.
/// Garantit la conformité syntaxique des données entrantes.
/// </summary>
public class CreateMaintenanceTicketValidator : AbstractValidator<CreateMaintenanceTicketCommand>
{
    public CreateMaintenanceTicketValidator()
    {
        // Stoppe les règles en cascade sur UNE MÊME propriété
        // Ex : si Title est vide → MaximumLength ne s'exécute pas
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(command => command.AssetId)
            .NotEmpty()
            .WithMessage("L'identifiant de l'actif cible (AssetId) est obligatoire.");

        RuleFor(command => command.Title)
            .NotEmpty()
            .WithMessage("Le titre du ticket est obligatoire.")
            .MaximumLength(150)
            .WithMessage("Le titre du ticket ne doit pas dépasser 150 caractères.");

        RuleFor(command => command.Description)
            .NotEmpty()
            .WithMessage("La description détaillée de l'anomalie est obligatoire.");

        RuleFor(command => command.Criticality)
            .NotEmpty()
            .WithMessage("Le niveau de criticité est obligatoire.")
            .WithMessage("La criticité fournie n'est pas valide. Valeurs autorisées : Low, Medium, High.");
    }
}