using AssetFlowCore.Application.Interfaces.RAG;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI;
using System.ClientModel;

namespace AssetFlowCore.Infrastructure.RAG;

/// <summary>
/// Extension methods that register all RAG / AI services into the DI container
/// and configure the Semantic Kernel builder for the AssetFlow Core application.
/// </summary>
public static class OllamaSemanticKernelSetup
{
    /// <summary>
    /// Registers the full RAG infrastructure stack:
    /// <list type="bullet">
    ///   <item><description><see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> backed by Ollama via the standardised Microsoft.Extensions.AI abstraction.</description></item>
    ///   <item><description>A Semantic Kernel <see cref="Kernel"/> wired to the local Ollama chat endpoint.</description></item>
    ///   <item><description>Application-layer RAG interfaces bound to their infrastructure implementations.</description></item>
    /// </list>
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <param name="config">The application configuration provider.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddOllamaRagServices(this IServiceCollection services, IConfiguration config)
    {
        // Récupération et validation des clés de configuration
        var ollamaBaseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var ollamaChatModel = config["Ollama:ChatModel"] ?? "mistral";
        var ollamaEmbedModel = config["Ollama:EmbeddingModel"] ?? "nomic-embed-text";

        // Normalisation de l'endpoint pour la compatibilité stricte de l'API OpenAI v1 d'Ollama
        var endpointUri = new Uri($"{ollamaBaseUrl.TrimEnd('/')}/v1/");

        // Instanciation d'un client unique partagé pour éviter les ouvertures de sockets redondantes
        var sharedOpenAIClient = new OpenAIClient(
            new ApiKeyCredential("ollama"),
            new OpenAIClientOptions { Endpoint = endpointUri });

        // Enregistrement du client pour des besoins d'infrastructure avancés optionnels
        services.AddSingleton(sharedOpenAIClient);

        // ----------------------------------------------------------------
        // 1.  Embedding generator
        // ----------------------------------------------------------------
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            return sharedOpenAIClient
                .GetEmbeddingClient(ollamaEmbedModel)
                .AsIEmbeddingGenerator();
        });

        // ----------------------------------------------------------------
        // 2.  Semantic Kernel & Chat Completion
        // ----------------------------------------------------------------
        services.AddScoped<Kernel>(sp =>
        {
            var builder = Kernel.CreateBuilder();

            builder.AddOpenAIChatCompletion(
                modelId: ollamaChatModel,
                openAIClient: sharedOpenAIClient);

            return builder.Build();
        });

        services.AddScoped<IChatCompletionService>(sp =>
            sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>());

        // ----------------------------------------------------------------
        // 3.  RAG infrastructure services
        // ----------------------------------------------------------------
        services.AddSingleton<ILocalVectorStore, LocalVectorStore>();
        services.AddScoped<IAIAssistanceGenerator, AIAssistanceGenerator>();
        services.AddTransient<IOllamaConnectivityService, OllamaConnectivityService>();

        return services;
    }
}