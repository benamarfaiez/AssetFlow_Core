using AssetFlowCore.Application.DTOs;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Tickets.CreateTicket;

public record CreateMaintenanceTicketCommand(Guid AssetId, string Title, string Description, string Criticality) : IRequest<TicketResponseDto>;
