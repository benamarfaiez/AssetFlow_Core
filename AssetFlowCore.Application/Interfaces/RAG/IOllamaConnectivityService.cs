using AssetFlowCore.Application.Models.RAG;

namespace AssetFlowCore.Application.Interfaces.RAG;

/// <summary>
/// Contract for checking the health and capabilities of a local Ollama instance.
/// </summary>
public interface IOllamaConnectivityService
{
    /// <summary>
    /// Checks whether the Ollama daemon is reachable and responding.
    /// </summary>
    /// <returns><c>true</c> if Ollama is alive and responding; <c>false</c> otherwise.</returns>
    Task<bool> IsAliveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the list of models currently available on the local Ollama instance.
    /// </summary>
    /// <returns>An ordered list of <see cref="OllamaModelInfo"/> descriptors.</returns>
    Task<IReadOnlyList<OllamaModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default);
}