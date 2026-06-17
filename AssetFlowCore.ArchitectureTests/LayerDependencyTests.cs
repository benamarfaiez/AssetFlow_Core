using ArchUnitNET.xUnit;
using Xunit;

using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace AssetFlowCore.ArchitectureTests;

public class LayerDependencyTests : BaseArchitectureTest
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Any_Other_Layer()
    {
        // Le cœur métier ne doit rien connaître de l'extérieur
        Types()
            .That().Are(DomainLayer)
            .Should()
            .NotDependOnAny(ApplicationLayer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

        Types()
            .That().Are(DomainLayer)
            .Should()
            .NotDependOnAny(InfrastructureLayer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

        Types()
            .That().Are(DomainLayer)
            .Should()
            .NotDependOnAny(WebApiLayer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Fact]
    public void Application_Should_Only_Depend_On_Domain()
    {
        // L'application orchestre mais ne dépend ni de la DB (Infra), ni du protocole (WebApi)
        Types()
            .That().Are(ApplicationLayer)
            .Should()
            .NotDependOnAny(InfrastructureLayer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

        Types()
            .That().Are(ApplicationLayer)
            .Should()
            .NotDependOnAny(WebApiLayer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_WebApi()
    {
        // L'infrastructure implémente les interfaces mais ignore l'existence de l'API
        var rule = Types()
            .That().Are(InfrastructureLayer)
            .Should()
            .NotDependOnAny(WebApiLayer)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }
}
