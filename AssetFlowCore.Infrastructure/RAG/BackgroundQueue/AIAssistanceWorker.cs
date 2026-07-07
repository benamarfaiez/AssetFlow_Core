using AssetFlowCore.Application.Interfaces.RAG;
using AssetFlowCore.Application.Models.RAG;
using AssetFlowCore.Domain.Repositories;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AssetFlowCore.Infrastructure.RAG.BackgroundQueue;

public sealed class AIAssistanceWorker(
    IAIAssistanceQueue queue,
    IServiceProvider serviceProvider,
    ILogger<AIAssistanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Le Worker RAG natif en tâche de fond a démarré.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Attente d'un ticket dans le Channel
                var ticketId = await queue.DequeueTicketAsync(stoppingToken);
                logger.LogInformation("Prise en charge RAG directe pour le ticket : {TicketId}", ticketId);

                // Utilisation de CreateAsyncScope() + await using pour libérer proprement le LocalVectorStore (IAsyncDisposable)
                await using var scope = serviceProvider.CreateAsyncScope();

                // Récupération des composants d'infrastructure requis depuis le scope
                var vectorStore = scope.ServiceProvider.GetRequiredService<ILocalVectorStore>();
                var embeddingGenerator = scope.ServiceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
                var aiGenerator = scope.ServiceProvider.GetRequiredService<IAIAssistanceGenerator>();
                var ticketRepository = scope.ServiceProvider.GetRequiredService<IMaintenanceTicketRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // Résolution facultative du service de connectivité (Uniquement présent si mode local Ollama actif)
                var connectivityService = scope.ServiceProvider.GetService<IOllamaConnectivityService>();
                if (connectivityService != null)
                {
                    bool isAlive = await connectivityService.IsAliveAsync(stoppingToken);
                    if (!isAlive)
                    {
                        logger.LogWarning("Démon Ollama indisponible. Ré-injection du ticket {TicketId} dans la file.", ticketId);
                        await queue.QueueTicketAsync(ticketId);
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                        continue;
                    }
                }

                var ticket = await ticketRepository.GetByIdAsync(ticketId, stoppingToken);

                if (ticket != null)
                {
                    if (string.IsNullOrWhiteSpace(ticket.Description))
                    {
                        logger.LogWarning("Le ticket avec l'ID {TicketId} n'a pas de description valide pour le traitement RAG.", ticketId);
                        ticket.FailAiProcessing();
                        await unitOfWork.SaveChangesAsync(stoppingToken);
                        continue;
                    }

                    try
                    {
                        await vectorStore.InitializeAsync(stoppingToken);

                        // FLUX RAG ÉTAPE A : Vectorisation sémantique de la description
                        var embeddingResult = await embeddingGenerator.GenerateAsync([ticket.Description], cancellationToken: stoppingToken);
                        var queryVector = embeddingResult[0].Vector.ToArray();

                        // FLUX RAG ÉTAPE B : Recherche de similarité cosinus dans DuckDB
                        var vectorResults = await vectorStore.SearchAsync(queryVector, topK: 3, threshold: 0.7f, stoppingToken);

                        // Mapping des vecteurs DuckDB en objets métiers lisibles par le rédacteur IA
                        var similarTickets = vectorResults.Select(v => new SimilarTicketResult(
                            TicketId: v.Id,
                            Description: v.Metadata.TryGetValue("Description", out var t) ? t.ToString()! : "Ticket Description",
                            Resolution: v.Metadata.TryGetValue("Resolution", out var s) ? s.ToString()! : "Résolution standard",
                            SimilarityScore: v.Score
                        ));

                        var suggestedProcedures = Enumerable.Empty<ResolutionProcedure>();

                        // FLUX RAG ÉTAPE C : Génération de la note Markdown par le LLM (Configuré via IChatCompletionService)
                        logger.LogDebug("Appel au modèle IA pour la rédaction de la note du ticket {TicketId}...", ticketId);
                        string markdownNote = await aiGenerator.GenerateAssistanceNoteAsync(
                            ticket.Description,
                            similarTickets,
                            suggestedProcedures,
                            stoppingToken);

                        // Sauvegarde du résultat, mise à jour du ticket et persistance SQL
                        ticket.SetAssistanceNote(markdownNote);
                        await unitOfWork.SaveChangesAsync(stoppingToken);

                        logger.LogInformation("Analyse RAG terminée et enregistrée avec succès pour le ticket {TicketId}.", ticketId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Échec du traitement RAG direct en tâche de fond pour le ticket {TicketId}.", ticketId);
                        ticket.FailAiProcessing();
                        await unitOfWork.SaveChangesAsync(stoppingToken);
                    }
                }
                else
                {
                    logger.LogWarning("Le ticket avec l'ID {TicketId} n'existe pas ou a été supprimé avant traitement RAG.", ticketId);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur critique non gérée dans la boucle principale du AIAssistanceWorker.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        logger.LogInformation("Le Worker RAG natif en tâche de fond s'est arrêté proprement.");
    }
}