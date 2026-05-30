using AssetFlowCore.Application.UseCases.Assets.RegisterAsset;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using System.Threading.Tasks;

namespace AssetFlowCore.Benchmarks.Application.UseCases;

/// <summary>
/// Mesure la performance du cas d'utilisation RegisterAsset de bout en bout :
/// validation du numéro de série + persistance InMemory + mapping DTO.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class RegisterAssetBenchmark : BenchmarkBase
{
    private int _counter;

    [GlobalSetup]
    public void Setup() => SetupServices("Bench_RegisterAsset");

    [IterationSetup]
    public void IterationSetup() => _counter++;

    [Benchmark(Baseline = true, Description = "Register Server")]
    public async Task RegisterServer()
    {
        var handler = Resolve<RegisterAssetHandler>();
        var cmd = new RegisterAssetCommand(
            $"Serveur-{_counter}",
            $"SRV-{_counter:D6}",
            "Server");
        await handler.HandleAsync(cmd);
    }

    [Benchmark(Description = "Register Laptop")]
    public async Task RegisterLaptop()
    {
        var handler = Resolve<RegisterAssetHandler>();
        var cmd = new RegisterAssetCommand(
            $"Laptop-{_counter}",
            $"LPT-{_counter:D6}",
            "Laptop");
        await handler.HandleAsync(cmd);
    }

    [Benchmark(Description = "Register NetworkDevice")]
    public async Task RegisterNetworkDevice()
    {
        var handler = Resolve<RegisterAssetHandler>();
        var cmd = new RegisterAssetCommand(
            $"Switch-{_counter}",
            $"SWI-{_counter:D6}",
            "NetworkDevice");
        await handler.HandleAsync(cmd);
    }
}