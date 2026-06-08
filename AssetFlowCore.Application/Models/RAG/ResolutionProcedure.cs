namespace AssetFlowCore.Application.Models.RAG;

/// <summary>
/// A resolution procedure suggested by the RAG pipeline for the current ticket context.
/// </summary>
/// <param name="ProcedureId">Unique identifier of the procedure document.</param>
/// <param name="Title">Human-readable title of the procedure.</param>
/// <param name="Steps">Ordered list of steps that constitute the procedure.</param>
/// <param name="RelevanceScore">Score indicating how relevant this procedure is to the query.</param>
public sealed record ResolutionProcedure(
    string ProcedureId,
    string Title,
    IReadOnlyList<string> Steps,
    float RelevanceScore);