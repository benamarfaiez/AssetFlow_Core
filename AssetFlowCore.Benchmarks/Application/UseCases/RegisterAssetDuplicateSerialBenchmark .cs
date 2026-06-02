using AssetFlowCore.Application.UseCases.Assets.RegisterAsset;
using AssetFlowCore.Domain.Exceptions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace AssetFlowCore.Benchmarks.Application.UseCases;

/// <summary>
/// Mesure le coût de la détection de doublon de numéro de série dans RegisterAsset.
/// Compare le chemin heureux (serial unique) vs le chemin rejeté (serial existant).
/// ExistsWithSerialNumberAsync effectue un AnyAsync EF Core — important à mesurer.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, iterationCount: 3)]
[RankColumn]
public class RegisterAssetDuplicateSerialBenchmark : BenchmarkBase
{
    private const string ExistingSerial = "SRV-DUPLICATE-01";
    private int _counter;

    [GlobalSetup]
    public async Task Setup()
    {
        SetupServices("Bench_Duplicate_Serial");

        // Pré-enregistre un asset avec le serial qui sera dupliqué
        var handler = Resolve<RegisterAssetHandler>();
        await handler.HandleAsync(new RegisterAssetCommand("Asset Existant", ExistingSerial, "Server"));
    }

    [IterationSetup]
    public void Increment() => _counter++;

    [Benchmark(Baseline = true, Description = "Register — serial unique (succès)")]
    public async Task Register_UniqueSerial()
    {
        var handler = Resolve<RegisterAssetHandler>();
        await handler.HandleAsync(new RegisterAssetCommand(
            $"Asset-{_counter}", $"UNIQUE-{_counter:D6}", "Server"));
    }

    [Benchmark(Description = "Register — serial dupliqué (DomainException + AnyAsync)")]
    public async Task Register_DuplicateSerial()
    {
        var handler = Resolve<RegisterAssetHandler>();
        try
        {
            await handler.HandleAsync(new RegisterAssetCommand(
                "Doublon", ExistingSerial, "Laptop"));
        }
        catch (DomainException)
        {
            // Attendu — mesure le coût du chemin rejeté (ExistsWithSerialNumberAsync retourne true)
        }
    }

    [Benchmark(Description = "Register — serial avec espaces et casse mixte (normalisation)")]
    public async Task Register_NormalizedSerial()
    {
        var handler = Resolve<RegisterAssetHandler>();
        await handler.HandleAsync(new RegisterAssetCommand(
            $"Asset-Norm-{_counter}", $"  norm-{_counter:D6}  ", "NetworkDevice"));
    }
}