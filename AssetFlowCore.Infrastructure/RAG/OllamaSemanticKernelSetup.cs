using AssetFlowCore.Application.Interfaces.RAG;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    /// Registers the full RAG infrastructure stack with aligned service lifetimes.
    /// </summary>
    public static IServiceCollection AddOllamaRagServices(this IServiceCollection services, IConfiguration config)
    {
        // 1. Récupération et validation des clés de configuration
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
        // 2. Embedding generator (Générateur d'embeddings Microsoft.Extensions.AI)
        // ----------------------------------------------------------------
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            return sharedOpenAIClient
                .GetEmbeddingClient(ollamaEmbedModel)
                .AsIEmbeddingGenerator();
        });

        // ----------------------------------------------------------------
        // 3. Semantic Kernel & Chat Completion (Alignés en Scoped)
        // ----------------------------------------------------------------
        services.AddScoped<Kernel>(sp =>
        {
            var builder = Kernel.CreateBuilder();

            builder.AddOpenAIChatCompletion(
                modelId: ollamaChatModel,
                openAIClient: sharedOpenAIClient);

            // Permet au Kernel d'utiliser le système de log applicatif
            builder.Services.AddSingleton(sp.GetRequiredService<ILoggerFactory>());

            return builder.Build();
        });

        // Enregistrement direct et propre du service de Chat sans forcer une double résolution
        services.AddScoped<IChatCompletionService>(sp =>
        {
            var kernel = sp.GetRequiredService<Kernel>();
            return kernel.GetRequiredService<IChatCompletionService>();
        });

        // ----------------------------------------------------------------
        // 4. RAG infrastructure services
        // ----------------------------------------------------------------

        services.AddScoped<ILocalVectorStore, LocalVectorStore>();

        services.AddScoped<IAIAssistanceGenerator, AIAssistanceGenerator>();
        services.AddTransient<IOllamaConnectivityService, OllamaConnectivityService>();

        return services;
    }
}