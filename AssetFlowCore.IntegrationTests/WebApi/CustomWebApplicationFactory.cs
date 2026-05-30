using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AssetFlowCore.Infrastructure.Persistence;
using System.Linq;

namespace AssetFlowCore.IntegrationTests.WebApi;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 1. Supprimer l'enregistrement du DbContext
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AssetFlowDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // 2. Nettoyer AUSSI l'enregistrement générique des DbContextOptions (le coupable de votre erreur !)
            var optionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions));
            if (optionsDescriptor != null)
            {
                services.Remove(optionsDescriptor);
            }

            // 3. Réenregistrer proprement le DbContext configuré UNIQUEMENT pour l'InMemory
            services.AddDbContext<AssetFlowDbContext>(options =>
            {
                options.UseInMemoryDatabase("IntegrationTestsDb");
                options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });
        });
    }

    //protected override void ConfigureWebHost(IWebHostBuilder builder)
    //{
    //    builder.ConfigureServices(services =>
    //    {
    //        // 1. On retire la configuration existante de SQL Server
    //        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AssetFlowDbContext>));
    //        if (descriptor != null) services.Remove(descriptor);

    //        // 2. On réenregistre l'InMemory en lui ordonnant de s'isoler
    //        services.AddDbContext<AssetFlowDbContext>((container, options) =>
    //        {
    //            options.UseInMemoryDatabase("IntegrationTestsDb");

    //            // TRUC DE SIOUX : On force EF Core à ignorer le ServiceProvider global pour ses composants internes
    //            options.UseRootApplicationServiceProvider();

    //            options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
    //        });
    //    });
    //}
}