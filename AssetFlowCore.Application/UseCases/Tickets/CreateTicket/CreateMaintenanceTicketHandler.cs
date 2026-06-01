using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using FluentValidation;

namespace AssetFlowCore.Application.UseCases.Tickets.CreateTicket;

/// <summary>
/// Chef d'orchestre du cas d'utilisation "Déclaration d'un incident".
/// Respecte le principe SRP (Single Responsibility Principle) en ne gérant qu'un seul scénario métier.
/// </summary>
/// <remarks>
/// Constructeur injectant uniquement des abstractions (Inversion des dépendances - SOLID "D").
/// </remarks>
public class CreateMaintenanceTicketHandler(
    IAssetRepository assetRepository,
    IMaintenanceTicketRepository ticketRepository,
    IUnitOfWork unitOfWork,
    ITicketAssignmentEngine assignmentEngine,
    INotificationService notificationService,
    IValidator<CreateMaintenanceTicketCommand> validator)
{

    /// <summary>
    /// Exécute de manière transactionnelle l'ouverture du ticket et la mutation de l'actif lié.
    /// </summary>
    public async Task<TicketResponseDto> HandleAsync(CreateMaintenanceTicketCommand command)
    {
        // Valide toutes les propriétés — collecte toutes les erreurs en une fois
        var validationResult = await validator.ValidateAsync(command);

        if (!validationResult.IsValid)
        {
            // Regroupe toutes les erreurs dans une seule DomainException lisible
            var errors = string.Join(" | ", validationResult.Errors
                .Select(e => e.ErrorMessage));

            throw new DomainException(errors);
        }

        // 1. Récupération de l'agrégat / entité cible
        var asset = await assetRepository.GetByIdAsync(command.AssetId) ?? throw new DomainException($"L'actif cible {command.AssetId} n'existe pas.");

        // 2. Validation des invariants métiers du Domaine
        if (asset.Status == AssetStatus.Decommissioned)
            throw new DomainException("Opération interdite : impossible d'ouvrir un incident sur un actif mis au rebut.");

        // 3. Traduction de la primitive Web en Énumération fortement typée du Domaine
        var criticality = Enum.Parse<TicketCriticality>(command.Criticality, ignoreCase: true);

        // 4. Utilisation du moteur algorithmique pour résoudre l'équipe technique d'astreinte (Pattern Strategy)
        string assignedTeam = assignmentEngine.ResolveTeam(asset.Type, criticality);

        // 5. Instanciation de la nouvelle entité de maintenance
        var ticket = new MaintenanceTicket(
            id: Guid.NewGuid(),
            assetId: asset.Id,
            title: command.Title,
            description: command.Description,
            criticality: criticality,
            assignedTeam: assignedTeam
        );

        // 6. Déclenchement de l'automate d'état en cascade (L'actif passe automatiquement à "Down")
        asset.MarkAsDown();

        // 7. Notification des repositories (Suivi en mémoire par le Change Tracker d'EF Core)
        await ticketRepository.AddAsync(ticket);

        // 8. PERSISTANCE ATOMIQUE (Unit of Work)
        await unitOfWork.SaveChangesAsync();

        // 9. Traduction manuelle en DTO de surface (Zéro réflexion CPU au runtime)
        var dto = ticket.ToDto();

        // 10. Notification Temps Réel asynchrone et découplée (SignalR WebSockets)
        await notificationService.NotifyTeamNewTicketAsync(assignedTeam, dto);

        return dto;
    }
}