namespace AssetFlowCore.Application.UseCases.Tickets.TransferTicket;

public record RequestTicketTransferCommand(Guid TicketId, string TargetTeam, string Reason);
