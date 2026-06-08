using AssetFlowCore.Application.Models.RAG;

namespace AssetFlowCore.Application.Interfaces.RAG;

/// <summary>
/// Abstraction for a local, embedded vector store.
/// Implementations are infrastructure concerns (e.g. DuckDB).
/// </summary>
public interface ILocalVectorStore
{
    /// <summary>
    /// Ensures the underlying storage (table, schema) is created and ready.
    /// Safe to call multiple times (idempotent).
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates a vector entry identified by <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Stable business identifier (e.g. ticket GUID as string).</param>
    /// <param name="embedding">Dense float vector produced by the embedding model.</param>
    /// <param name="metadata">Arbitrary key-value pairs serialised as JSON alongside the vector.</param>
    Task UpsertVectorAsync(
        string id,
        float[] embedding,
        Dictionary<string, object> metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a cosine-similarity search and returns the <paramref name="topK"/> nearest neighbours
    /// whose similarity score exceeds <paramref name="threshold"/>.
    /// </summary>
    /// <param name="queryEmbedding">The query vector to compare against the store.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="threshold">Minimum cosine similarity score (0 – 1).</param>
    Task<IReadOnlyCollection<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        int topK,
        float threshold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently removes the vector entry with the given <paramref name="id"/>.
    /// No-op if the entry does not exist.
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}