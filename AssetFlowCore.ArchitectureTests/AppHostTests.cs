using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AssetFlowCore.ArchitectureTests
{
    public class AppHostTests
    {
        [Fact]
        public async Task AppHost_Configures_Expected_Resources_Correctly()
        {
            // Arrange : Initialise l'application Aspire en mode test
            var appBuilder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AssetFlowCore_Aspire_AppHost>();

            // Act
            using var app = await appBuilder.BuildAsync();
            var resources = app.Services.GetRequiredService<DistributedApplicationModel>().Resources;

            // Assert 1 : Vérifier la présence du serveur SQL (généralement une resource de type SqlServerServerResource)
            var sqlServerExists = resources.Any(r => r.Name == "sqlserver" || r.GetType().Name.Contains("SqlServer"));
            Assert.True(sqlServerExists, "Le composant SqlServer n'a pas été enregistré dans l'AppHost.");

            // Assert 2 : Vérifier la présence et le type du projet WebApi
            var webApiProject = resources.FirstOrDefault(r => r.Name.Contains("WebApi", StringComparison.OrdinalIgnoreCase));

            Assert.NotNull(webApiProject);
            // On vérifie que c'est bien un projet exécutable et non une simple chaîne de texte
            Assert.Contains("ProjectResource", webApiProject.GetType().Name);
        }
    }
}