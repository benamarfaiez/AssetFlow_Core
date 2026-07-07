using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Application.Interfaces.RAG;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Infrastructure.Cache;
using AssetFlowCore.Infrastructure.Configuration;
using AssetFlowCore.Infrastructure.Notifications;
using AssetFlowCore.Infrastructure.Persistence;
using AssetFlowCore.Infrastructure.Persistence.Repositories;
using AssetFlowCore.Infrastructure.RAG;
using AssetFlowCore.Infrastructure.RAG.BackgroundQueue;
using AssetFlowCore.Infrastructure.RAG.Providers.Ollama;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenAI;
using System.ClientModel;

namespace AssetFlowCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Outils techniques requis par les services
        services.AddMemoryCache();
        services.AddSignalR();

        // 2. Configuration du Options
        services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.SectionName);

        // 3. AssetRepository (Pattern Décorateur propre via IoC)
        services.AddScoped<AssetRepository>();
        services.AddScoped<IAssetRepository>(provider =>
            new CachedAssetRepository(
                provider.GetRequiredService<AssetRepository>(),
                provider.GetRequiredService<IMemoryCache>()
            ));

        // 4. TeamRepository
        services.AddScoped<TeamRepository>();
        services.AddScoped<ITeamRepository>(provider =>
            new CachedTeamRepository(
                provider.GetRequiredService<TeamRepository>(),
                provider.GetRequiredService<IMemoryCache>()
            ));

        // 5. Autres repositories et Unité de Travail
        services.AddScoped<IMaintenanceTicketRepository, MaintenanceTicketRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 6. Services d'infrastructure transverses
        services.AddScoped<IDbContextFactory, SqlServerDbContextFactory>();
        services.AddScoped<INotificationService, SignalRNotificationService>();

        // 6. Services RAG (Retrieval-Augmented Generation) et file d'attente pour l'assistance IA
        services.AddScoped<ILocalVectorStore, LocalVectorStore>();
        services.AddScoped<IAIAssistanceGenerator, AIAssistanceGenerator>();

        var useAzure = configuration.GetValue<bool>("AiSettings:UseAzure");
        if (useAzure)
        {
            ConfigureAzureOpenAi(services, configuration);
        }
        else
        {
            ConfigureLocalOllama(services, configuration);
        }

        services.AddSingleton<IAIAssistanceQueue, AIAssistanceQueue>();
        services.AddHostedService<AIAssistanceWorker>();

        return services;
    }
    private static void ConfigureAzureOpenAi(IServiceCollection services, IConfiguration config)
    {
        var endpoint = config["AzureOpenAi:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("Endpoint Azure manquant.");
        }

        var apiKey = config["AzureOpenAi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("ApiKey Azure manquante.");
        }
        var chatModel = config["AzureOpenAi:ChatDeploymentName"] ?? "gpt-4o";
        var embedModel = config["AzureOpenAi:EmbeddingDeploymentName"] ?? "text-embedding-3-small";

        // 1. Embeddings Azure via OpenAIClient standard (Méthode de compatibilité robuste)
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            // Le SDK attend uniquement l'URI racine de la ressource
            var azureClient = new AzureOpenAIClient(
                new Uri(endpoint),
                new ApiKeyCredential(apiKey));

            // .AsIEmbeddingGenerator() sait comment extraire le sous-client d'embeddings Azure
            return azureClient.GetEmbeddingClient(embedModel).AsIEmbeddingGenerator();
        });

        // 2. Chat Completion & Kernel
        services.AddScoped<Kernel>(sp =>
        {
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(chatModel, endpoint, apiKey);
            builder.Services.AddSingleton(sp.GetRequiredService<ILoggerFactory>());
            return builder.Build();
        });

        services.AddScoped<IChatCompletionService>(sp => sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>());
    }

    private static void ConfigureLocalOllama(IServiceCollection services, IConfiguration config)
    {
        var baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var chatModel = config["Ollama:ChatModel"] ?? "phi4";
        var embedModel = config["Ollama:EmbeddingModel"] ?? "nomic-embed-text";

        var sharedOpenAIClient = new OpenAIClient(
            new ApiKeyCredential("ollama"),
            new OpenAIClientOptions { Endpoint = new Uri($"{baseUrl.TrimEnd('/')}/v1/") });

        services.AddSingleton(sharedOpenAIClient);

        // 1. Embeddings locaux
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
            sharedOpenAIClient.GetEmbeddingClient(embedModel).AsIEmbeddingGenerator());

        // 2. Chat Completion & Kernel
        services.AddScoped<Kernel>(sp =>
        {
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(chatModel, sharedOpenAIClient);
            builder.Services.AddSingleton(sp.GetRequiredService<ILoggerFactory>());
            return builder.Build();
        });

        services.AddScoped<IChatCompletionService>(sp => sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>());

        // Le service de connectivité n'est injecté QUE pour Ollama
        services.AddTransient<IOllamaConnectivityService, OllamaConnectivityService>();
    }
}