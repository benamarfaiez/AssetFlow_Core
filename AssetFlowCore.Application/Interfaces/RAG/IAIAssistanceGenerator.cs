using AssetFlowCore.Application.Models.RAG;

namespace AssetFlowCore.Application.Interfaces.RAG;

/// <summary>
/// Contract for generating AI-assisted content for maintenance tickets.
/// Implementations rely on a generative LLM (e.g. Mistral via Ollama).
/// </summary>
public interface IAIAssistanceGenerator
{
    /// <summary>
    /// Generates a structured assistance note for a new ticket based on its description
    /// and the context retrieved by the RAG pipeline (similar past tickets and suggested procedures).
    /// </summary>
    /// <param name="ticketDescription">Raw description provided by the technician when opening the ticket.</param>
    /// <param name="similarTickets">Top-K semantically similar tickets retrieved from the vector store.</param>
    /// <param name="suggestedProcedures">Resolution procedures surfaced by the retrieval step.</param>
    /// <returns>
    /// A structured markdown string containing recommended diagnostic steps,
    /// relevant past-ticket context and suggested resolution actions.
    /// </returns>
    Task<string> GenerateAssistanceNoteAsync(
        string ticketDescription,
        IEnumerable<SimilarTicketResult> similarTickets,
        IEnumerable<ResolutionProcedure> suggestedProcedures,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a concise, professional resolution summary once a ticket is closed.
    /// Suitable for knowledge-base archiving and future RAG retrieval.
    /// </summary>
    /// <param name="description">Original ticket description.</param>
    /// <param name="resolution">Free-text resolution note written by the technician.</param>
    /// <returns>A polished, structured summary of the incident and its resolution.</returns>
    Task<string> GenerateResolutionSummaryAsync(
        string description,
        string resolution,
        CancellationToken cancellationToken = default);
}