using MediatR;

namespace AssetFlowCore.Application.UseCases.Tickets.AssignTicket;

public record AssignTicketToTechnicianCommand(Guid TicketId) : IRequest;