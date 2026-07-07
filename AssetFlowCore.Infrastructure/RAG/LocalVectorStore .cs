using AssetFlowCore.Application.Interfaces.RAG;
using AssetFlowCore.Application.Models.RAG;
using DuckDB.NET.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AssetFlowCore.Infrastructure.RAG;

public sealed class LocalVectorStore : ILocalVectorStore, IAsyncDisposable
{
    private const string TableName = "rag_vectors";
    private const string CosineSimilaritySql = """
        list_dot_product(embedding, CAST($query AS FLOAT[])) 
        / (sqrt(list_dot_product(embedding, embedding)) 
        * sqrt(list_dot_product(CAST($query AS FLOAT[]), CAST($query AS FLOAT[]))))
        """;

    private readonly DuckDBConnection _connection;
    private readonly ILogger<LocalVectorStore> _logger;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public LocalVectorStore(IConfiguration config, ILogger<LocalVectorStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var dataPath = config["VectorStore:DataPath"] ?? "./vectordb";
        Directory.CreateDirectory(dataPath);

        _connection = new DuckDBConnection($"Data Source={Path.Combine(dataPath, "tickets.duckdb")}");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            if (_connection.State != System.Data.ConnectionState.Open)
            {
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }

            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {TableName} (
                id VARCHAR PRIMARY KEY,
                embedding FLOAT[],
                metadata JSON,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            """;
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            _initialized = true;
            _logger.LogInformation("DuckDB Vector Store initialisé avec succès.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'initialisation de DuckDB.");
            throw;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<IReadOnlyCollection<VectorSearchResult>> SearchAsync(float[] queryEmbedding, int topK, float threshold, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        var sql = $"""
            SELECT id, metadata, {CosineSimilaritySql} AS score
            FROM {TableName}
            WHERE {CosineSimilaritySql} >= $threshold
            ORDER BY score DESC
            LIMIT $topK;
            """;

        try
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new DuckDBParameter("query", FormatFloatArray(queryEmbedding)));
            cmd.Parameters.Add(new DuckDBParameter("threshold", threshold));
            cmd.Parameters.Add(new DuckDBParameter("topK", topK));

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var results = new List<VectorSearchResult>();

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var rowId = reader.GetString(0);
                var metaJson = reader.IsDBNull(1) ? "{}" : reader.GetString(1);
                var score = Convert.ToSingle(reader.GetValue(2));

                var meta = JsonSerializer.Deserialize<Dictionary<string, object>>(metaJson) ?? [];
                results.Add(new VectorSearchResult(rowId, score, meta));
            }

            return results.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'exécution de la recherche vectorielle DuckDB.");
            throw;
        }
    }

    public async Task UpsertVectorAsync(string id, float[] embedding, Dictionary<string, object> metadata, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        const string sql = $"""
            INSERT INTO {TableName} (id, embedding, metadata, created_at)
            VALUES ($id, $embedding, $metadata, CURRENT_TIMESTAMP)
            ON CONFLICT (id) DO UPDATE
                SET embedding = excluded.embedding,
                    metadata = excluded.metadata,
                    created_at = excluded.created_at;
            """;

        try
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new DuckDBParameter("id", id));
            cmd.Parameters.Add(new DuckDBParameter("embedding", FormatFloatArray(embedding)));
            cmd.Parameters.Add(new DuckDBParameter("metadata", JsonSerializer.Serialize(metadata)));

            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du Upsert du vecteur ID {Id}.", id);
            throw;
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        const string sql = $"DELETE FROM {TableName} WHERE id = $id;";

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new DuckDBParameter("id", id));
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!_initialized) await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string FormatFloatArray(float[] values)
        => "[" + string.Join(", ", values.Select(v => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture))) + "]";

    public async ValueTask DisposeAsync()
    {
        _initLock.Dispose();
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}