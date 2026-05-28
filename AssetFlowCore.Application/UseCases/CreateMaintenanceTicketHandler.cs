using AssetFlowCore.Application.DTOs;
using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.UseCases;

public class CreateMaintenanceTicketHandler
{
    private readonly IAssetRepository _assetRepository;
    private readonly IMaintenanceTicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITicketAssignmentEngine _assignmentEngine;
    private readonly INotificationService _notificationService;

    public CreateMaintenanceTicketHandler(
        IAssetRepository assetRepository,
        IMaintenanceTicketRepository ticketRepository,
        IUnitOfWork unitOfWork,
        ITicketAssignmentEngine assignmentEngine,
        INotificationService notificationService)
    {
        _assetRepository = assetRepository;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _assignmentEngine = assignmentEngine;
        _notificationService = notificationService;
    }

    public async Task<TicketResponseDto> HandleAsync(CreateMaintenanceTicketCommand command)
    {
        var asset = await _assetRepository.GetByIdAsync(command.AssetId);
        if (asset == null)
            throw new DomainException($"L'actif cible {command.AssetId} n'existe pas.");

        if (asset.Status == Domain.Enums.AssetStatus.Decommissioned)
            throw new DomainException("Opération interdite sur un actif mis au rebut.");

        var criticality = Enum.Parse<TicketCriticality>(command.Criticality, ignoreCase: true);
        string team = _assignmentEngine.ResolveTeam(asset.Type, criticality);

        var ticket = new MaintenanceTicket(Guid.NewGuid(), asset.Id, command.Title, command.Description, criticality, team);

        asset.MarkAsDown(); // Automate en cascade

        await _ticketRepository.AddAsync(ticket);
        await _unitOfWork.SaveChangesAsync();

        var dto = ticket.ToDto();
        await _notificationService.NotifyTeamNewTicketAsync(team, dto); // Temps réel découplé

        return dto;
    }
}
