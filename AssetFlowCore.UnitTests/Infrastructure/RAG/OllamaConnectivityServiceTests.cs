using AssetFlowCore.Infrastructure.RAG;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RestSharp;
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
        // Configuration in-memory minimale pour le constructeur
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Ollama:BaseUrl", BaseUrl }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    /// <summary>
    /// Helper pour créer le service tout en injectant un HttpMessageHandler simulé (Mock)
    /// afin d'intercepter les requêtes HTTP de RestSharp.
    /// </summary>
    private OllamaConnectivityService CreateServiceWithMockHttpMessageHandler(HttpResponseMessage mockResponse)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // 1. Setup existant pour l'envoi de la requête HTTP
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // 2. AJOUT : Autoriser le nettoyage (Dispose) du handler par RestSharp sans lever d'exception
        handlerMock
            .Protected()
            .Setup("Dispose", ItExpr.IsAny<bool>())
            .Verifiable(); // Permet de dire que cet appel est valide et attendu

        var service = new OllamaConnectivityService(_configuration, _loggerMock.Object);

        var testOptions = new RestClientOptions(BaseUrl)
        {
            Timeout = TimeSpan.FromSeconds(5),
            ConfigureMessageHandler = _ => handlerMock.Object
        };

        var testClient = new RestClient(testOptions);
        var clientField = typeof(OllamaConnectivityService).GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var oldClient = (RestClient)clientField!.GetValue(service)!;
        oldClient?.Dispose();

        clientField.SetValue(service, testClient);

        return service;
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
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network down"));

        // 1. Instanciation initiale du service
        var service = new OllamaConnectivityService(_configuration, _loggerMock.Object);

        // 2. Préparation des NOUVELLES options avec notre Handler simulé (AVANT la création du client)
        var testOptions = new RestClientOptions(BaseUrl)
        {
            Timeout = TimeSpan.FromSeconds(5),
            ConfigureMessageHandler = _ => handlerMock.Object // Assignation valide ici !
        };

        // 3. Création du client de test
        var testClient = new RestClient(testOptions);

        // 4. Remplacement par Réflexion pour contourner le ReadOnly
        var clientField = typeof(OllamaConnectivityService).GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Libération de l'ancien client pour éviter les fuites de sockets
        var oldClient = (RestClient)clientField!.GetValue(service)!;
        oldClient?.Dispose();

        // Injection du client configuré pour le test
        clientField.SetValue(service, testClient);

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
        // Doit être trié par nom (codellama en premier, mistral en second)
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
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection timed out"));

        var service = new OllamaConnectivityService(_configuration, _loggerMock.Object);

        // Configurer les options de test en FORÇANT la levée d'exceptions
        var testOptions = new RestClientOptions(BaseUrl)
        {
            Timeout = TimeSpan.FromSeconds(5),
            ConfigureMessageHandler = _ => handlerMock.Object,
            ThrowOnAnyError = true
        };

        var testClient = new RestClient(testOptions);

        var clientField = typeof(OllamaConnectivityService).GetField("_client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var oldClient = (RestClient)clientField!.GetValue(service)!;
        oldClient?.Dispose();

        clientField.SetValue(service, testClient);

        // Act & Assert
        await service.Invoking(s => s.ListModelsAsync(CancellationToken.None))
            .Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Connection timed out");
    }

    #endregion
}