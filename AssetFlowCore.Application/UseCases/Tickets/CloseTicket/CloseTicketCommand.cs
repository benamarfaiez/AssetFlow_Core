using MediatR;

namespace AssetFlowCore.Application.UseCases.Tickets.CloseTicket;

public record CloseTicketCommand(Guid TicketId, string ResolutionComment) : IRequest;