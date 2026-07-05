using AssetFlowCore.Application.DTOs;
using MediatR;

namespace AssetFlowCore.Application.UseCases.Tickets.GetTicket;

public record GetTicketQuery(Guid TicketId) : IRequest<TicketResponseDto>;