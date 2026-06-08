using AssetFlowCore.Application.Interfaces.RAG;
using System.Threading.Channels;

namespace AssetFlowCore.Infrastructure.RAG.BackgroundQueue;


public sealed class AIAssistanceQueue : IAIAssistanceQueue
{
    // Channel non borné optimisé pour un scénario multi-producteurs (API) et mono-consommateur (Worker)
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
    {
        SingleReader = true,
        AllowSynchronousContinuations = false
    });

    public ValueTask QueueTicketAsync(Guid ticketId)
    {
        return _queue.Writer.WriteAsync(ticketId);
    }

    public ValueTask<Guid> DequeueTicketAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAsync(cancellationToken);
    }
}