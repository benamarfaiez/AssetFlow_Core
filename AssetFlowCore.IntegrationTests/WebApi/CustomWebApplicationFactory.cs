using AssetFlowCore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace AssetFlowCore.IntegrationTests.WebApi;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 1. On cherche et on supprime TOUS les services liés de près ou de loin à AssetFlowDbContext
            // (Y compris le pool, les options et le contexte lui-même injectés par Aspire)
            var aspireDescriptors = services.Where(d =>
                d.ServiceType == typeof(AssetFlowDbContext) ||
                d.ServiceType == typeof(DbContextOptions<AssetFlowDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType.FullName.Contains("AssetFlowDbContext")).ToList();

            foreach (var descriptor in aspireDescriptors)
            {
                services.Remove(descriptor);
            }

            // 2. Création d'un fournisseur de services isolé pour EF Core In-Memory
            var internalServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // 3. Ré-enregistrement standard propre (Scoped) pour les tests d'intégration
            // Cette méthode ré-injecte l'écosystème EF Core sain de zéro
            services.AddDbContext<AssetFlowDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestsDb")
                       .UseInternalServiceProvider(internalServiceProvider);
            });
        });
    }
}