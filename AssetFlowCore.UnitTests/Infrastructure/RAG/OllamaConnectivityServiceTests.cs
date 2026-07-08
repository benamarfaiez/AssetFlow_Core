using AssetFlowCore.Application.Interfaces.RAG;
using AssetFlowCore.Infrastructure.RAG.Providers.Ollama;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace AssetFlowCore.UnitTests.Infrastructure.RAG;

public class OllamaConnectivityServiceTests
{
    private readonly Mock<ILogger<OllamaConnectivityService>> _loggerMock = new();
    private readonly IConfiguration _configuration;
    private const string BaseUrl = "http://localhost:11434";

    public OllamaConnectivityServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Ollama:BaseUrl", BaseUrl }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    /// <summary>
    /// Helper optimisé utilisant le constructeur interne de couplage de test (sans réflexion).
    /// </summary>
    private OllamaConnectivityService CreateServiceWithMockHttpMessageHandler(HttpResponseMessage mockResponse, bool throwOnAnyError = false)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        handlerMock
            .Protected()
            .Setup("Dispose", ItExpr.IsAny<bool>())
            .Verifiable();

        // Utilisation directe du constructeur internal prévu pour intercepter les options RestClient
        return new OllamaConnectivityService(_configuration, _loggerMock.Object, options =>
        {
            options.ConfigureMessageHandler = _ => handlerMock.Object;
            options.ThrowOnAnyError = throwOnAnyError;
        });
    }

    /// <summary>
    /// Helper pour simuler une levée d'exception réseau directe du HttpMessageHandler.
    /// </summary>
    private OllamaConnectivityService CreateServiceWithFaultyHandler(Exception exceptionToThrow, bool throwOnAnyError = false)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exceptionToThrow);

        return new OllamaConnectivityService(_configuration, _loggerMock.Object, options =>
        {
            options.ConfigureMessageHandler = _ => handlerMock.Object;
            options.ThrowOnAnyError = throwOnAnyError;
        });
    }

    #region Tests pour IsAliveAsync

    [Fact]
    public async Task IsAliveAsync_ShouldReturnTrue_WhenOllamaReturns200Ok()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK);
        using var service = CreateServiceWithMockHttpMessageHandler(mockResponse);

        // Act
        var result = await service.IsAliveAsync(CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAliveAsync_ShouldReturnFalse_WhenOllamaReturnsError()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        using var service = CreateServiceWithMockHttpMessageHandler(mockResponse);

        // Act
        var result = await service.IsAliveAsync(CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAliveAsync_ShouldReturnFalse_WhenHttpRequestExceptionIsThrown()
    {
        // Arrange
        using var service = CreateServiceWithFaultyHandler(new HttpRequestException("Network down"));

        // Act
        var result = await service.IsAliveAsync(CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Tests pour ListModelsAsync

    [Fact]
    public async Task ListModelsAsync_ShouldReturnSortedList_WhenPayloadIsValid()
    {
        // Arrange
        var fakeOllamaPayload = new
        {
            models = new[]
            {
                new { name = "mistral:latest", modified_at = DateTimeOffset.UtcNow, size = 4100000000L },
                new { name = "codellama:7b", modified_at = DateTimeOffset.UtcNow.AddDays(-1), size = 3800000000L }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(fakeOllamaPayload);
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };

        using var service = CreateServiceWithMockHttpMessageHandler(mockResponse);

        // Act
        var result = await service.ListModelsAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull().And.HaveCount(2);
        result[0].Name.Should().Be("codellama:7b");
        result[1].Name.Should().Be("mistral:latest");
        result[1].SizeBytes.Should().Be(4100000000L);
    }

    [Fact]
    public async Task ListModelsAsync_ShouldReturnEmptyList_WhenResponseIsEmptyOrError()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("")
        };
        using var service = CreateServiceWithMockHttpMessageHandler(mockResponse);

        // Act
        var result = await service.ListModelsAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListModelsAsync_ShouldThrowInvalidOperationException_WhenJsonIsCorrupted()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ malformed json : [ }")
        };
        using var service = CreateServiceWithMockHttpMessageHandler(mockResponse);

        // Act & Assert
        await service.Invoking(s => s.ListModelsAsync(CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Ollama returned an unexpected response format for /api/tags.");
    }

    [Fact]
    public async Task ListModelsAsync_ShouldRethrow_WhenNetworkErrorOccurs()
    {
        // Arrange & Act
        using var service = CreateServiceWithFaultyHandler(new HttpRequestException("Connection timed out"), throwOnAnyError: true);

        // Assert
        await service.Invoking(s => s.ListModelsAsync(CancellationToken.None))
            .Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Connection timed out");
    }

    #endregion

    #region Tests pour la couverture de ConfigureAzureOpenAi

    [Fact]
    public void AddInfrastructure_ShouldExecuteConfigureAzureOpenAi_WhenUseAzureIsTrue()
    {
        // Arrange
        var services = new ServiceCollection();

        // On prépare les configurations pour simuler le mode Azure OpenAI
        var azureSettings = new Dictionary<string, string?>
        {
            { "AiSettings:UseAzure", "true" },
            { "AzureOpenAi:Endpoint", "https://assetflow-test.openai.azure.com/" },
            { "AzureOpenAi:ApiKey", "une-cle-api-de-test-123" },
            { "AzureOpenAi:ChatDeploymentName", "gpt-4o" },
            { "AzureOpenAi:EmbeddingDeploymentName", "text-embedding-3-small" },
            { "VectorStore:DataPath", "./test_duckdb_path" },
            { "DatabaseOptions:SectionName", "Test" } // Évite un crash lors du binding des options
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(azureSettings)
            .Build();

        services.AddLogging();

        // Act - Appel direct de la classe que vous venez de me montrer
        AssetFlowCore.Infrastructure.DependencyInjection.AddInfrastructure(services, configuration);
        var provider = services.BuildServiceProvider();

        // Assert - On valide le bon enregistrement des briques configurées par ConfigureAzureOpenAi
        var chatService = provider.GetService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
        chatService.Should().NotBeNull();

        var embeddingGenerator = provider.GetService<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>();
        embeddingGenerator.Should().NotBeNull();

        var kernel = provider.GetService<Microsoft.SemanticKernel.Kernel>();
        kernel.Should().NotBeNull();

        // On vérifie que la connectivité Ollama est bien restée à NULL en mode Azure
        var connectivityService = provider.GetService<IOllamaConnectivityService>();
        connectivityService.Should().BeNull();
    }

    [Theory]
    [InlineData("AzureOpenAi:Endpoint", "Endpoint Azure manquant.")]
    [InlineData("AzureOpenAi:ApiKey", "ApiKey Azure manquante.")]
    public void AddInfrastructure_ConfigureAzureOpenAi_ShouldThrow_WhenConfigIsMissing(string missingKey, string expectedMessage)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var azureSettings = new Dictionary<string, string?>
        {
            { "AiSettings:UseAzure", "true" },
            { "AzureOpenAi:Endpoint", "https://assetflow-test.openai.azure.com/" },
            { "AzureOpenAi:ApiKey", "une-cle-api-de-test-123" }
        };

        // Enlever la clé ciblée pour forcer le throw de vos gardes de sécurité
        azureSettings.Remove(missingKey);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(azureSettings)
            .Build();

        // Act & Assert
        Action act = () => AssetFlowCore.Infrastructure.DependencyInjection.AddInfrastructure(services, configuration);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage(expectedMessage);
    }

    #endregion
}