using AssetFlowCore.Application.Interfaces.RAG;
using AssetFlowCore.Application.Models.RAG;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetFlowCore.Infrastructure.RAG;

/// <summary>
/// Checks the availability of the local Ollama daemon and queries its model catalogue
/// using the Ollama HTTP REST API.
/// </summary>
public sealed class OllamaConnectivityService : IOllamaConnectivityService, IDisposable
{
    // ── Ollama API endpoints ──────────────────────────────────────────────────
    private const string HealthEndpoint = "/";
    private const string ModelsEndpoint = "/api/tags";

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    // ── State ─────────────────────────────────────────────────────────────────
    private readonly string _baseUrl;
    private readonly RestClient _client;
    private readonly ILogger<OllamaConnectivityService> _logger;

    // ── Constructor ───────────────────────────────────────────────────────────
    public OllamaConnectivityService(IConfiguration config, ILogger<OllamaConnectivityService> logger)
        : this(config, logger, null) // Appelle le constructeur interne ci-dessous
    {
    }

    internal OllamaConnectivityService(
        IConfiguration config,
        ILogger<OllamaConnectivityService> logger,
        Action<RestClientOptions>? configureOptions)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(config);
        _logger = logger;
        _baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";

        var options = new RestClientOptions(_baseUrl)
        {
            Timeout = DefaultTimeout
        };

        // Permet l'injection du Mock de test (HttpMessageHandler)
        configureOptions?.Invoke(options);

        _client = new RestClient(options);
    }

    // ── IOllamaConnectivityService ────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<bool> IsAliveAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Probing Ollama health at {BaseUrl}{Endpoint}.", _baseUrl, HealthEndpoint);

        try
        {
            var request = new RestRequest(HealthEndpoint, Method.Get);
            var response = await _client.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            var isAlive = response.IsSuccessful;

            if (isAlive)
                _logger.LogInformation("Ollama daemon is alive at {BaseUrl}.", _baseUrl);
            else
                _logger.LogWarning(
                    "Ollama health probe returned HTTP {StatusCode} ({StatusDescription}).",
                    (int)response.StatusCode, response.StatusDescription);

            return isAlive;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Ollama daemon is unreachable at {BaseUrl}.", _baseUrl);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while probing Ollama health.");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OllamaModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching Ollama model list from {BaseUrl}{Endpoint}.", _baseUrl, ModelsEndpoint);

        try
        {
            var request = new RestRequest(ModelsEndpoint, Method.Get);
            var response = await _client.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning(
                    "Failed to retrieve Ollama model list. HTTP {StatusCode}: {Body}.",
                    (int)response.StatusCode,
                    response.Content ?? "<empty>");

                return [];
            }

            var payload = JsonSerializer.Deserialize<OllamaTagsResponse>(
                response.Content!,
                OllamaJsonOptions.Default);

            if (payload?.Models is null or { Count: 0 })
            {
                _logger.LogInformation("Ollama returned an empty model list.");
                return [];
            }

            var models = payload.Models
                .Where(m => m != null)
                .Select(m => new OllamaModelInfo(
                    Name: m.Name ?? "<unknown>",
                    ModifiedAt: m.ModifiedAt,
                    SizeBytes: m.Size))
                .OrderBy(m => m.Name)
                .ToList()
                .AsReadOnly();

            _logger.LogInformation(
                "Ollama model list retrieved: {Count} model(s) found ({Names}).",
                models.Count,
                string.Join(", ", models.Select(m => m.Name)));

            return models;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialise the Ollama model list response.");
            throw new InvalidOperationException("Ollama returned an unexpected response format for /api/tags.", ex);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Network error while fetching Ollama model list.");
            throw;
        }
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose() => _client.Dispose();

    // ── Internal DTOs (Ollama /api/tags response) ─────────────────────────────

    private sealed class OllamaTagsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModelEntry>? Models { get; init; }
    }

    private sealed class OllamaModelEntry
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("modified_at")]
        public DateTimeOffset ModifiedAt { get; init; }

        [JsonPropertyName("size")]
        public long Size { get; init; }
    }

    // ── JSON options ──────────────────────────────────────────────────────────

    private static class OllamaJsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}