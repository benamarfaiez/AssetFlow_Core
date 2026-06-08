namespace AssetFlowCore.Application.Models.RAG;

/// <summary>
/// Metadata descriptor for a model hosted on a local Ollama instance.
/// </summary>
/// <param name="Name">Model name as registered in Ollama (e.g. "mistral", "nomic-embed-text").</param>
/// <param name="ModifiedAt">UTC timestamp of the last pull / update for this model.</param>
/// <param name="SizeBytes">Disk size of the model in bytes.</param>
public sealed record OllamaModelInfo(
    string Name,
    DateTimeOffset ModifiedAt,
    long SizeBytes);