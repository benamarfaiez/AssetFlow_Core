using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Infrastructure;
using AssetFlowCore.Infrastructure.Cache;
using AssetFlowCore.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetFlowCore.UnitTests.Infrastructure;

/// <summary>
/// Couvre la correction 1.1 au niveau de la composition : l'unité de travail instanciait
/// elle-même ses dépôts (<c>new AssetRepository(context)</c>), ce qui court-circuitait les
/// décorateurs de cache et laissait les écritures sans invalidation.
/// </summary>
public class UnitOfWorkCompositionTests
{
    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiSettings:UseAzure"] = "false",
                ["Ollama:BaseUrl"] = "http://localhost:11434"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AssetFlowDbContext>(options =>
            options.UseInMemoryDatabase($"UoWComposition_{Guid.NewGuid()}"));
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }

    [Fact]
    public void UnitOfWork_ShouldExposeCachedRepositories_ResolvedFromContainer()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        unitOfWork.Asset.Should().BeOfType<CachedAssetRepository>(
            "les écritures d'inventaire doivent traverser le décorateur de cache");
        unitOfWork.Team.Should().BeOfType<CachedTeamRepository>(
            "les écritures d'équipe doivent traverser le décorateur de cache");
    }

    [Fact]
    public void UnitOfWork_ShouldShareTheSameRepositoryInstancesAsDirectInjection()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var assetRepository = scope.ServiceProvider.GetRequiredService<IAssetRepository>();
        var teamRepository = scope.ServiceProvider.GetRequiredService<ITeamRepository>();

        // Un handler injectant directement le dépôt et le même handler passant par l'unité de
        // travail doivent voir le même état de cache au sein d'une requête.
        unitOfWork.Asset.Should().BeSameAs(assetRepository);
        unitOfWork.Team.Should().BeSameAs(teamRepository);
    }
}
