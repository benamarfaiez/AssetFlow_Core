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
        var sampleEmbedding = new float[] { 1.0f, 0.0f, 0.0f };
        var metadata = new Dictionary<string, object> { { "Description", "Panne réseau" } };

        // Act
        await store.UpsertVectorAsync(vectorId, sampleEmbedding, metadata);
        var searchResults = await store.SearchAsync(sampleEmbedding, topK: 1, threshold: 0.5f);

        // Assert
        searchResults.Should().ContainSingle();
        searchResults.First().Id.Should().Be(vectorId);
        searchResults.First().Score.Should().BeGreaterThan(0.99f);

        await store.DisposeAsync();
    }

    // ── NOUVEAUX TESTS POUR COUVERTURE MAXIMALE ───────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ShouldRemoveVector_WhenIdExists()
    {
        // Arrange
        var store = new LocalVectorStore(_configuration, _loggerMock.Object);
        var vectorId = "ticket_to_delete";
        var sampleEmbedding = new float[] { 0.0f, 1.0f, 0.0f };
        var metadata = new Dictionary<string, object> { { "Description", "A supprimer" } };

        await store.UpsertVectorAsync(vectorId, sampleEmbedding, metadata);

        // Vérification préventive de la présence
        var checkBefore = await store.SearchAsync(sampleEmbedding, topK: 1, threshold: 0.5f);
        checkBefore.Should().ContainSingle();

        // Act
        await store.DeleteAsync(vectorId, CancellationToken.None);

        // Assert : La recherche ne doit plus rien renvoyer
        var checkAfter = await store.SearchAsync(sampleEmbedding, topK: 1, threshold: 0.5f);
        checkAfter.Should().BeEmpty();

        await store.DisposeAsync();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenIdDoesNotExist()
    {
        // Arrange
        var store = new LocalVectorStore(_configuration, _loggerMock.Object);

        // Act & Assert
        await store.Invoking(s => s.DeleteAsync("non_existing_id", CancellationToken.None))
            .Should().NotThrowAsync();

        await store.DisposeAsync();
    }

    [Theory]
    [InlineData(0, 0.5f)]   // topK invalide (doit être > 0)
    [InlineData(-1, 0.5f)]  // topK invalide
    [InlineData(5, -0.1f)]  // threshold invalide (< 0)
    [InlineData(5, 1.1f)]   // threshold invalide (> 1)
    public async Task SearchAsync_ShouldThrowArgumentOutOfRangeException_WhenArgumentsAreInvalid(int topK, float threshold)
    {
        // Arrange
        var store = new LocalVectorStore(_configuration, _loggerMock.Object);
        var query = new float[] { 1.0f, 0.0f, 0.0f };

        // Act & Assert
        await store.Invoking(s => s.SearchAsync(query, topK, threshold, CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();

        await store.DisposeAsync();
    }

    [Fact]
    public async Task SearchAsync_ShouldAutoInitialize_WhenCalledWithoutExplicitInit()
    {
        // Arrange
        var store = new LocalVectorStore(_configuration, _loggerMock.Object);
        var sampleEmbedding = new float[] { 0.5f, 0.5f };

        // Act & Assert : Ne doit pas lever d'erreur car `EnsureInitializedAsync` fait le travail
        await store.Invoking(s => s.SearchAsync(sampleEmbedding, topK: 2, threshold: 0.1f, CancellationToken.None))
            .Should().NotThrowAsync();

        await store.DisposeAsync();
    }

    public void Dispose()
    {
        // Forcer le Garbage Collector à libérer les handles de fichiers DuckDB persistants si nécessaire
        GC.Collect();
        GC.WaitForPendingFinalizers();

        if (Directory.Exists(_testDataPath))
        {
            try { Directory.Delete(_testDataPath, true); } catch { /* Ignoré si verrouillé */ }
        }
        GC.SuppressFinalize(this);
    }
}