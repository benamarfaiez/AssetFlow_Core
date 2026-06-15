namespace AssetFlowCore.Application.Interfaces.RAG;

public interface IAIAssistanceQueue
{
    ValueTask QueueTicketAsync(Guid ticketId);
    ValueTask<Guid> DequeueTicketAsync(CancellationToken cancellationToken);
}