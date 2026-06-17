using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace AssetFlowCore.ArchitectureTests;

public abstract class BaseArchitectureTest
{
    // Chargement des DLLs du projet
    protected static readonly Architecture Architecture = new ArchLoader()
            .LoadAssemblies(
                typeof(AssetFlowCore.Domain.Entities.Asset).Assembly,
                typeof(AssetFlowCore.Application.DependencyInjection).Assembly,
                typeof(AssetFlowCore.Infrastructure.DependencyInjection).Assembly,
                typeof(AssetFlowCore.WebApi.Middlewares.ExceptionHandlingMiddleware).Assembly
            )
            .Build();

    // Définition des couches par Espace de Noms
    protected static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInNamespaceMatching("AssetFlowCore.Domain").As("DomainLayer");

    protected static IObjectProvider<Class> ApplicationLayer =>
        Classes().That().ResideInNamespaceMatching("AssetFlowCore.Application.*").As("ApplicationLayer");

    protected static IObjectProvider<Class> InfrastructureLayer =>
        Classes().That().ResideInNamespaceMatching("AssetFlowCore.Infrastructure.*").As("InfrastructureLayer");

    protected static IObjectProvider<Class> WebApiLayer =>
        Classes().That().ResideInNamespaceMatching("AssetFlowCore.WebApi.*").As("WebApiLayer");
}
