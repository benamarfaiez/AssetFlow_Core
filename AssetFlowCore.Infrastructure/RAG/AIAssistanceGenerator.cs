using AssetFlowCore.Application.Interfaces.RAG;
using AssetFlowCore.Application.Models.RAG;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AssetFlowCore.Infrastructure.RAG;

/// <summary>
/// Generates AI-assisted notes and resolution summaries for maintenance tickets
/// by invoking a local Mistral model via the Semantic Kernel chat completion pipeline.
/// </summary>
/// <remarks>
/// Uses C# 12 primary constructors.
/// The <see cref="Kernel"/> is intentionally NOT injected here; only the
/// <see cref="IChatCompletionService"/> is required, keeping the service lean
/// and independently testable.
/// </remarks>
public sealed class AIAssistanceGenerator(
    IChatCompletionService chatCompletionService,
    ILogger<AIAssistanceGenerator> logger)
    : IAIAssistanceGenerator
{
    // ── Execution settings (Mistral on Ollama) ────────────────────────────────

    private static OpenAIPromptExecutionSettings AssistanceSettings => new()
    {
        Temperature = 0.7,
        MaxTokens = 1024
    };

    private static OpenAIPromptExecutionSettings SummarySettings => new()
    {
        Temperature = 0.3,   // lower temperature for more deterministic summaries
        MaxTokens = 512
    };

    // ── IAIAssistanceGenerator ────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<string> GenerateAssistanceNoteAsync(
        string ticketDescription,
        IEnumerable<SimilarTicketResult> similarTickets,
        IEnumerable<ResolutionProcedure> suggestedProcedures,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ticketDescription);

        logger.LogInformation(
            "Generating AI assistance note for ticket (description length: {Length} chars).",
            ticketDescription.Length);

        var systemPrompt = BuildAssistanceSystemPrompt();
        var userPrompt = BuildAssistanceUserPrompt(ticketDescription, similarTickets, suggestedProcedures);

        try
        {
            var history = new ChatHistory();
            history.AddSystemMessage(systemPrompt);
            history.AddUserMessage(userPrompt);

            var response = await chatCompletionService
                .GetChatMessageContentAsync(history, AssistanceSettings, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var content = response.Content ?? string.Empty;

            logger.LogInformation(
                "AI assistance note generated successfully ({Tokens} tokens used).",
                response.Metadata?.TryGetValue("Usage", out var usage) == true ? usage : "unknown");

            return content;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to generate AI assistance note for ticket description starting with: '{Preview}'.",
                ticketDescription[..Math.Min(80, ticketDescription.Length)]);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> GenerateResolutionSummaryAsync(
        string description,
        string resolution,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolution);

        logger.LogInformation(
            "Generating resolution summary (description length: {DescLen}, resolution length: {ResLen}).",
            description.Length, resolution.Length);

        try
        {
            var history = new ChatHistory();
            history.AddSystemMessage(BuildSummarySystemPrompt());
            history.AddUserMessage(BuildSummaryUserPrompt(description, resolution));

            var response = await chatCompletionService
                .GetChatMessageContentAsync(history, SummarySettings, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var content = response.Content ?? string.Empty;

            logger.LogInformation("Resolution summary generated successfully.");

            return content;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to generate resolution summary for ticket starting with: '{Preview}'.",
                description[..Math.Min(80, description.Length)]);
            throw;
        }
    }

    // ── Prompt builders ───────────────────────────────────────────────────────

    private static string BuildAssistanceSystemPrompt() =>
        """
        You are an expert IT maintenance assistant embedded in the AssetFlow Core system.
        Your role is to help technicians diagnose and resolve hardware and infrastructure issues quickly.

        When provided with a ticket description and context from similar past incidents,
        you MUST produce a structured assistance note in the following markdown format:

        ## 🔍 Diagnostic Steps
        (ordered list of recommended diagnostic actions)

        ## 🔗 Relevant Past Incidents
        (brief mention of the most relevant similar tickets and their resolutions)

        ## ✅ Suggested Resolution
        (step-by-step resolution based on past procedures and context)

        ## ⚠️ Escalation Triggers
        (list any conditions that warrant immediate escalation to a senior engineer)

        Be concise, technical and actionable. Respond only in the same language as the ticket description.
        """;

    private static string BuildAssistanceUserPrompt(
        string description,
        IEnumerable<SimilarTicketResult> similarTickets,
        IEnumerable<ResolutionProcedure> procedures)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("## New Ticket Description");
        sb.AppendLine(description);
        sb.AppendLine();

        var ticketList = similarTickets.ToList();
        if (ticketList.Count > 0)
        {
            sb.AppendLine("## Similar Past Incidents (most relevant first)");
            foreach (var t in ticketList)
            {
                sb.AppendLine($"- **[{t.TicketId}]** (similarity: {t.SimilarityScore:P0})");
                sb.AppendLine($"  Description: {t.Description}");
                if (!string.IsNullOrWhiteSpace(t.Resolution))
                    sb.AppendLine($"  Resolution : {t.Resolution}");
            }
            sb.AppendLine();
        }

        var procedureList = procedures.ToList();
        if (procedureList.Count > 0)
        {
            sb.AppendLine("## Suggested Resolution Procedures");
            foreach (var p in procedureList)
            {
                sb.AppendLine($"### {p.Title} (relevance: {p.RelevanceScore:P0})");
                for (var i = 0; i < p.Steps.Count; i++)
                    sb.AppendLine($"{i + 1}. {p.Steps[i]}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string BuildSummarySystemPrompt() =>
        """
        You are a technical writer for an IT asset management system.
        Your task is to produce a concise, professional resolution summary from a technician's raw notes.

        The summary MUST follow this markdown structure:

        ## Incident Summary
        (one paragraph, factual)

        ## Root Cause
        (identified root cause, one or two sentences)

        ## Resolution Applied
        (clear, numbered steps that were performed)

        ## Preventive Actions
        (recommended follow-up or preventive measures, if applicable)

        Keep the language neutral, precise and suitable for a knowledge base.
        Respond in the same language as the input.
        """;

    private static string BuildSummaryUserPrompt(string description, string resolution) =>
        $"""
         ## Original Ticket Description
         {description}

         ## Technician Resolution Notes
         {resolution}
         """;
}