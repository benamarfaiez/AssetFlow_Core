using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace AssetFlowCore.ArchitectureTests;

public class DesignAndNamingTests : BaseArchitectureTest
{
    // --- SÉCURISATION DU DOMAINE (DDD) ---
    [Fact]
    public void Domain_Entities_Should_Not_Have_Public_Setters()
    {
        IObjectProvider<Class> domainEntities = Classes()
            .That().Are(DomainLayer)
            .And().ResideInNamespaceMatching("AssetFlowCore.Domain.Entities.*")
            .As("DomainEntities");

        IArchRule rule_1 = PropertyMembers()
                    .That().AreDeclaredIn(domainEntities)
                    .Should().NotHavePublicSetter();
        rule_1.Check(Architecture);

        IArchRule rule_2 = PropertyMembers()
            .That().AreDeclaredIn(domainEntities)
            .Should().NotHaveProtectedSetter();
        rule_2.Check(Architecture);
    }

    // --- PATTERN CQRS ---
    [Fact]
    public void Cqrs_Handlers_Should_Reside_Only_In_Application_Layer()
    {
        IArchRule rule = Classes()
            .That().HaveNameEndingWith("Handler")
            .Should().ResideInNamespaceMatching("AssetFlowCore.Application.*");

        rule.Check(Architecture);
    }

    [Fact]
    public void Commands_And_Queries_Should_Be_Immutable()
    {
        IObjectProvider<Class> cqsObjects = Classes()
            .That().Are(ApplicationLayer)
            .And().HaveNameEndingWith("Command")
            .Or().HaveNameEndingWith("Query")
            .As("CqsObjects");

        IArchRule rule = PropertyMembers()
                    .That().AreDeclaredIn(cqsObjects)
                    .Should().BeImmutable();

        rule.Check(Architecture);
    }

    // --- COUPLAGE LÂCHE ---
    [Fact]
    public void Infrastructure_Repositories_Should_Implement_Interfaces_From_Domain_Or_Application()
    {
        IObjectProvider<IType> forbiddenInterfaces = Interfaces()
            .That().AreNot(DomainLayer)
            .And().AreNot(ApplicationLayer)
            .As("ForbiddenInterfaces");

        // Règle : Les repositories ne doivent pas dépendre de ces interfaces interdites
        IArchRule rule = Classes()
            .That().Are(InfrastructureLayer)
            .And().HaveNameEndingWith("Repository")
            .Should()
            .NotDependOnAny(forbiddenInterfaces);

        rule.Check(Architecture);
    }

    [Fact]
    public void WebApi_Controllers_Should_Never_Call_Repositories_Directly()
    {
        IObjectProvider<IType> repositories = Classes()
                    .That().Are(InfrastructureLayer)
                    .And().HaveNameEndingWith("Repository")
                    .As("Repositories");

        IArchRule rule = Types()
            .That().Are(WebApiLayer)
            .Should().NotDependOnAny(repositories);

        rule.Check(Architecture);
    }

    // --- STANDARDS DE NOMMAGE ---
    [Fact]
    public void All_Interfaces_Should_Start_With_I()
    {
        IArchRule rule = Interfaces()
            .That().Are(DomainLayer)
            .Or().Are(ApplicationLayer)
            .Should().HaveNameStartingWith("I");

        rule.Check(Architecture);
    }
}
