using AssetFlowCore.Domain.Exceptions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AssetFlowCore.Benchmarks.Infrastructure;

/// <summary>
/// Mesure le coût du ExceptionHandlingMiddleware pour chaque type d'exception géré.
/// Focus : coût de sérialisation JSON de ProblemDetails (RFC 7807) par type d'erreur.
/// Ce coût s'ajoute à chaque requête en erreur — important pour les scénarios de charge.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 3)]
[RankColumn]
public class ExceptionHandlingSerializationBenchmark
{
    private ProblemDetails _domainProblem = null!;
    private ProblemDetails _concurrencyProblem = null!;
    private ProblemDetails _serverErrorProblem = null!;

    [GlobalSetup]
    public void Setup()
    {
        _domainProblem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Règle métier violée",
            Detail = "Ce numéro de série constructeur est déjà enregistré dans le parc."
        };

        _concurrencyProblem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Concurrence d'accès détectée",
            Detail = "Cette ressource a été mise à jour par un autre utilisateur. Veuillez recharger les données."
        };

        _serverErrorProblem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erreur interne du serveur",
            Detail = "Une erreur imprévue est survenue sur notre serveur éditeur."
        };
    }

    [Benchmark(Baseline = true, Description = "Sérialise ProblemDetails 400 (DomainException)")]
    public string Serialize_DomainException()
        => JsonSerializer.Serialize(_domainProblem);

    [Benchmark(Description = "Sérialise ProblemDetails 409 (DbUpdateConcurrencyException)")]
    public string Serialize_ConcurrencyException()
        => JsonSerializer.Serialize(_concurrencyProblem);

    [Benchmark(Description = "Sérialise ProblemDetails 500 (Exception non gérée)")]
    public string Serialize_ServerError()
        => JsonSerializer.Serialize(_serverErrorProblem);

    [Benchmark(Description = "Switch pattern dispatch — DomainException")]
    public static int Dispatch_DomainException()
    {
        Exception ex = new DomainException("Série déjà enregistrée");
        return ex switch
        {
            DomainException => StatusCodes.Status400BadRequest,
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    [Benchmark(Description = "Switch pattern dispatch — DbUpdateConcurrencyException")]
    public static int Dispatch_ConcurrencyException()
    {
        Exception ex = new DbUpdateConcurrencyException();
        return ex switch
        {
            DomainException => StatusCodes.Status400BadRequest,
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}