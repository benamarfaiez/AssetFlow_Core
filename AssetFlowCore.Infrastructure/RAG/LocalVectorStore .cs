using AssetFlowCore.Application.Interfaces.RAG;
using AssetFlowCore.Application.Models.RAG;
using DuckDB.NET.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AssetFlowCore.Infrastructure.RAG;

/// <summary>
/// Embedded vector store backed by DuckDB.
/// The cosine similarity is computed entirely in SQL using DuckDB list functions,
/// avoiding any round-trips for scoring.
/// </summary>
public sealed class LocalVectorStore : ILocalVectorStore, IAsyncDisposable
{
    // ── Configuration ────────────────────────────────────────────────────────
    private const string TableName = "rag_vectors";

    // ── Cosine similarity SQL expression
    // list_dot_product / (norm(a) * norm(b))
    private const string CosineSimilaritySql =
        "list_dot_product(embedding, $query) " +
        "/ (sqrt(list_sum(list_multiply(embedding, embedding))) " +
        "* sqrt(list_sum(list_multiply($query, $query))))";

    // ── State ────────────────────────────────────────────────────────────────
    private readonly DuckDBConnection _connection;
    private readonly ILogger<LocalVectorStore> _logger;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    // ── Constructor ──────────────────────────────────────────────────────────
    public LocalVectorStore(IConfiguration config, ILogger<LocalVectorStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var dataPath = config["VectorStore:DataPath"] ?? "./vectordb";
        Directory.CreateDirectory(dataPath);

        var connectionString = $"Data Source={Path.Combine(dataPath, "tickets.duckdb")}";
        _connection = new DuckDBConnection(connectionString);
    }

    // ── ILocalVectorStore ────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return; // double-checked locking

            _logger.LogInformation("Initialising RAG vector store (table: {Table}).", TableName);

            const string ddl = $"""
                CREATE TABLE IF NOT EXISTS {TableName} (
                    id         VARCHAR     NOT NULL PRIMARY KEY,
                    embedding  FLOAT[]     NOT NULL,
                    metadata   JSON,
                    created_at TIMESTAMP   NOT NULL DEFAULT CURRENT_TIMESTAMP
                );
                """;

            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = ddl;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            _initialized = true;
            _logger.LogInformation("Vector store initialised successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialise the vector store table '{Table}'.", TableName);
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpsertVectorAsync(
        string id,
        float[] embedding,
        Dictionary<string, object> metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(embedding);
        ArgumentNullException.ThrowIfNull(metadata);

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Upserting vector for id={Id}, dim={Dim}.", id, embedding.Length);

        // Typage simplifié au niveau de l'insertion pour éviter les conflits de binding
        const string sql = $"""
            INSERT INTO {TableName} (id, embedding, metadata, created_at)
            VALUES ($id, $embedding, $metadata, CURRENT_TIMESTAMP)
            ON CONFLICT (id) DO UPDATE
                SET embedding   = excluded.embedding,
                    metadata    = excluded.metadata,
                    created_at  = excluded.created_at;
            """;

        try
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;

            cmd.Parameters.Add(new DuckDBParameter("id", id));
            cmd.Parameters.Add(new DuckDBParameter("embedding", FormatFloatArray(embedding)));
            cmd.Parameters.Add(new DuckDBParameter("metadata", JsonSerializer.Serialize(metadata)));

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Vector upserted successfully for id={Id}.", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert vector for id={Id}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        int topK,
        float threshold,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);
        if (topK <= 0) throw new ArgumentOutOfRangeException(nameof(topK), "topK must be positive.");
        if (threshold is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 0 and 1.");

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Searching vector store: topK={TopK}, threshold={Threshold}, dim={Dim}.",
            topK, threshold, queryEmbedding.Length);

        var sql = $"""
            SELECT
                id,
                metadata,
                {CosineSimilaritySql} AS score
            FROM {TableName}
            WHERE {CosineSimilaritySql} >= $threshold
            ORDER BY score DESC
            LIMIT $topK;
            """;

        try
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;

            var queryParam = FormatFloatArray(queryEmbedding);
            cmd.Parameters.Add(new DuckDBParameter("query", queryParam));
            cmd.Parameters.Add(new DuckDBParameter("threshold", threshold));
            cmd.Parameters.Add(new DuckDBParameter("topK", topK));

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var results = new List<VectorSearchResult>();

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var rowId = reader.GetString(0);
                var metaJson = reader.IsDBNull(1) ? "{}" : reader.GetString(1);
                var score = Convert.ToSingle(reader.GetValue(2));

                var meta = JsonSerializer.Deserialize<Dictionary<string, object>>(metaJson)
                           ?? [];

                results.Add(new VectorSearchResult(rowId, score, meta));
            }

            _logger.LogDebug("Vector search returned {Count} result(s).", results.Count);
            return results.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute vector similarity search.");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Deleting vector entry id={Id}.", id);

        const string sql = $"DELETE FROM {TableName} WHERE id = $id;";

        try
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new DuckDBParameter("id", id));

            var affected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Delete completed. Rows affected: {Rows}.", affected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete vector entry id={Id}.", id);
            throw;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!_initialized)
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts a float array to the DuckDB literal format expected by the FLOAT[] cast,
    /// e.g. [0.1, 0.2, 0.3].
    /// </summary>
    private static string FormatFloatArray(float[] values)
        => "[" + string.Join(", ", values.Select(v => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture))) + "]";

    // ── IAsyncDisposable ─────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        _initLock.Dispose();
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}