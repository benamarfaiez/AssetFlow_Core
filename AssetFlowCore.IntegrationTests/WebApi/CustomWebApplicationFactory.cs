using AssetFlowCore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssetFlowCore.IntegrationTests.WebApi;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 1. Nettoyage des anciens descripteurs de contexte
            var optionsDescriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AssetFlowDbContext>) ||
                d.ServiceType == typeof(AssetFlowDbContext) ||
                d.ServiceType == typeof(DbContextOptions)).ToList();

            // Supprimer TOUS les enregistrements de DbContextOptions<AssetFlowDbContext> (doublons inclus)
            foreach (var descriptor in optionsDescriptors)
            {
                services.Remove(descriptor);
            }

            // 2. CRÉATION D'UN FOURNISSEUR DE SERVICES ISOLÉ POUR EF CORE IN-MEMORY
            // Cela force EF Core à ne pas partager ses services internes avec ceux de SQL Server
            var internalServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // 3. Ré-enregistrement du DbContext en lui injectant ce fournisseur isolé
            services.AddDbContext<AssetFlowDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestsDb")
                       .UseInternalServiceProvider(internalServiceProvider);
            });
        });
    }
}