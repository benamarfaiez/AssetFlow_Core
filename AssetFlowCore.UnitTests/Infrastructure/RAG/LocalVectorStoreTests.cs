using AssetFlowCore.Infrastructure.RAG;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace AssetFlowCore.UnitTests.Infrastructure.RAG;

public class LocalVectorStoreTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<LocalVectorStore>> _loggerMock = new();

    public LocalVectorStoreTests()
    {
        // Génération d'un sous-dossier éphémère unique pour éviter les conflits d'I/O entre exécutions
        _testDataPath = Path.Combine(Path.GetTempPath(), $"duckdb_tests_{Guid.NewGuid():N}");

        var inMemorySettings = new Dictionary<string, string?> {
            {"VectorStore:DataPath", _testDataPath}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateDatabaseAndTable()
    {
        // Arrange
        var store = new LocalVectorStore(_configuration, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await store.InitializeAsync();

        // Assert
        await act.Should().NotThrowAsync();
        File.Exists(Path.Combine(_testDataPath, "tickets.duckdb")).Should().BeTrue();

        await store.DisposeAsync();
    }

    [Fact]
    public async Task UpsertAndSearch_ShouldStoreAndRetrieveEmbeddings()
    {
        // Arrange
        var store = new LocalVectorStore(_configuration, _loggerMock.Object);
        await store.InitializeAsync();

        var vectorId = "ticket_123";
        var sampleEmbedding = new float[] { 1.0f, 0.0f, 0.0f }; // Vecteur unitaire X
        var metadata = new Dictionary<string, object> { { "Description", "Panne réseau" } };

        // Act
        await store.UpsertVectorAsync(vectorId, sampleEmbedding, metadata);

        // Recherche avec le même vecteur (Similarité cosinus parfaite attendue = 1.0)
        var searchResults = await store.SearchAsync(sampleEmbedding, topK: 1, threshold: 0.5f);

        // Assert
        searchResults.Should().ContainSingle();
        searchResults.First().Id.Should().Be(vectorId);
        searchResults.First().Score.Should().BeGreaterThan(0.99f); // Proche de 1.0

        await store.DisposeAsync();
    }

    public void Dispose()
    {
        // Nettoyage complet du dossier de base de données temporaire
        if (Directory.Exists(_testDataPath))
        {
            try { Directory.Delete(_testDataPath, true); } catch { /* Ignoré si verrouillé */ }
        }
    }
}