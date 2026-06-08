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

        // 1. Attente d'un ticket dans le Channel
        var ticketId = await queue.DequeueTicketAsync(stoppingToken);
        logger.LogInformation("Prise en charge RAG directe pour le ticket : {TicketId}", ticketId);

        // 2. Ouverture du scope pour isoler les accès aux données (Dépôts SQL)
        using var scope = serviceProvider.CreateScope();

        // 3. Récupération des composants d'infrastructure requis
        var vectorStore = scope.ServiceProvider.GetRequiredService<ILocalVectorStore>();
        var embeddingGenerator = scope.ServiceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        var aiGenerator = scope.ServiceProvider.GetRequiredService<IAIAssistanceGenerator>();
        var ticketRepository = scope.ServiceProvider.GetRequiredService<IMaintenanceTicketRepository>();

        var ticket = await ticketRepository.GetByIdAsync(ticketId);
        if (ticket != null)
        {
            if (string.IsNullOrWhiteSpace(ticket?.Description))
            {
                throw new InvalidOperationException($"Le ticket avec l'ID {ticketId} n'a pas de description valide pour le traitement RAG.");
            }

            try
            {
                // 4. FLUX RAG ÉTAPE A : Vectorisation sémantique de la description (nomic-embed-text)
                var embeddingResult = await embeddingGenerator.GenerateAsync([ticket.Description], cancellationToken: stoppingToken);
                var queryVector = embeddingResult[0].Vector.ToArray();

                // 5. FLUX RAG ÉTAPE B : Recherche de similarité cosinus dans DuckDB
                var vectorResults = await vectorStore.SearchAsync(queryVector, topK: 3, threshold: 0.7f, stoppingToken);

                // 6. Mapping des vecteurs DuckDB en objets métiers lisibles par le rédacteur IA
                var similarTickets = vectorResults.Select(v => new SimilarTicketResult(
                    TicketId: v.Id,
                    Description: v.Metadata.TryGetValue("Description", out var t) ? t.ToString()! : "Ticket Description",
                    Resolution: v.Metadata.TryGetValue("Resolution", out var s) ? s.ToString()! : "Résolution standard",
                    SimilarityScore: v.Score
                ));

                // Collection optionnelle de procédures de maintenance (laisser vide ou injecter votre dépôt)
                var suggestedProcedures = Enumerable.Empty<ResolutionProcedure>();

                // 7. FLUX RAG ÉTAPE C : Génération de la note Markdown par le LLM (Mistral)
                logger.LogDebug("Appel au modèle local pour la rédaction de la note du ticket {TicketId}...", ticketId);
                string markdownNote = await aiGenerator.GenerateAssistanceNoteAsync(
                    ticket.Description,
                    similarTickets,
                    suggestedProcedures,
                    stoppingToken);

                // 8. Sauvegarde du résultat et mise à jour du ticket
                ticket.SetAssistanceNote(markdownNote);


                logger.LogInformation("Analyse RAG terminée et enregistrée avec succès pour le ticket {TicketId}.", ticketId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Échec du traitement RAG direct en tâche de fond.");
                ticket?.FailAiProcessing();
            }
        }
        else
        {
            throw new InvalidOperationException($"Le ticket avec l'ID {ticketId} n'existe pas ou a été supprimé.");
        }
    }
}