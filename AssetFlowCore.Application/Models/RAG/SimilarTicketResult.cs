namespace AssetFlowCore.Application.Models.RAG;

/// <summary>
/// A past ticket that is semantically similar to the current query ticket,
/// surfaced by the RAG retrieval step.
/// </summary>
/// <param name="TicketId">Unique identifier of the similar ticket.</param>
/// <param name="Description">Original description of the similar ticket.</param>
/// <param name="Resolution">How the similar ticket was resolved (if available).</param>
/// <param name="SimilarityScore">Cosine similarity score against the query.</param>
public sealed record SimilarTicketResult(
    string TicketId,
    string Description,
    string? Resolution,
    float SimilarityScore);
