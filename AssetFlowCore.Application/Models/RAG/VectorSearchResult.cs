namespace AssetFlowCore.Application.Models.RAG;

/// <summary>
/// Represents a single result returned by a vector similarity search.
/// </summary>
/// <param name="Id">The stable business identifier of the stored vector (e.g. ticket GUID).</param>
/// <param name="Score">Cosine similarity score between 0 and 1 (higher = more similar).</param>
/// <param name="Metadata">The arbitrary key-value metadata stored alongside the vector.</param>
public sealed record VectorSearchResult(
    string Id,
    float Score,
    IReadOnlyDictionary<string, object> Metadata);
