using AssetFlowCore.Application.UseCases.Assets.GetAllAssets;
using AssetFlowCore.Application.UseCases.Assets.RegisterAsset;
using AssetFlowCore.Application.UseCases.Tickets.CreateTicket;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace AssetFlowCore.Benchmarks.Infrastructure;

/// <summary>
/// Mesure le coût de résolution des handlers depuis le conteneur DI.
/// Chaque requête HTTP crée un nouveau scope — ce benchmark valide que
/// la résolution des dépendances chaînées (Handler → Repository → DbContext)
/// ne constitue pas un goulot d'étranglement.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class DependencyInjectionResolutionBenchmark : BenchmarkBase
{
    [GlobalSetup]
    public void Setup() => SetupServices("Bench_DI_Resolution");

    [Benchmark(Baseline = true, Description = "Résolution RegisterAssetHandler (scope frais)")]
    public RegisterAssetHandler Resolve_RegisterAssetHandler()
        => Resolve<RegisterAssetHandler>();

    [Benchmark(Description = "Résolution GetAllAssetsHandler (scope frais)")]
    public GetAllAssetsHandler Resolve_GetAllAssetsHandler()
        => Resolve<GetAllAssetsHandler>();

    [Benchmark(Description = "Résolution CreateMaintenanceTicketHandler (5 dépendances)")]
    public CreateMaintenanceTicketHandler Resolve_CreateTicketHandler()
        => Resolve<CreateMaintenanceTicketHandler>();

    [Benchmark(Description = "Création scope DI + résolution + dispose")]
    public async Task<bool> ScopeLifetime()
    {
        await using var scope = ServiceProvider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<RegisterAssetHandler>();
        return handler != null;
    }
}