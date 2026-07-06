using AssetFlowCore.Application.Interfaces.RAG;
using AssetFlowCore.Application.Models.RAG;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Enums;
using AssetFlowCore.Domain.Repositories;
using AssetFlowCore.Infrastructure.RAG.BackgroundQueue;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace AssetFlowCore.UnitTests.Infrastructure.RAG;

public class AIAssistanceWorkerTests
{
    private readonly Mock<IAIAssistanceQueue> _queueMock = new();
    private readonly Mock<ILocalVectorStore> _vectorStoreMock = new();
    private readonly Mock<IEmbeddingGenerator<string, Embedding<float>>> _embeddingMock = new();
    private readonly Mock<IAIAssistanceGenerator> _aiGeneratorMock = new();
    private readonly Mock<IMaintenanceTicketRepository> _ticketRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<AIAssistanceWorker>> _loggerMock = new();
    private readonly IServiceProvider _serviceProvider;

    public AIAssistanceWorkerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_vectorStoreMock.Object);
        services.AddSingleton(_embeddingMock.Object);
        services.AddSingleton(_aiGeneratorMock.Object);
        services.AddSingleton(_ticketRepoMock.Object);
        services.AddSingleton(_uowMock.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessSuccessfully_WhenTicketHasDescription()
    {
        // Arrange
        var ticketId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var ticket = new MaintenanceTicket(ticketId, assetId, "Écran cassé", "Description valide pour l'IA", TicketCriticality.Medium, teamId);

        var cts = new CancellationTokenSource();
        _queueMock.SetupSequence(q => q.DequeueTicketAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticketId)
            .ThrowsAsync(new OperationCanceledException());

        _ticketRepoMock.Setup(r => r.GetByIdAsync(ticketId, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var vector = new float[] { 0.1f, 0.2f, 0.3f };
        var embedding = new Embedding<float>(vector);
        var generatedEmbeddings = new GeneratedEmbeddings<Embedding<float>>([embedding]);

        _embeddingMock
            .Setup(e => e.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedEmbeddings);

        var searchResults = new List<VectorSearchResult>
        {
            new(Guid.NewGuid().ToString(), 0.85f, new Dictionary<string, object> { { "Description", "Ancien écran" }, { "Resolution", "Remplacement fait" } })
        };
        _vectorStoreMock.Setup(v => v.SearchAsync(It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults.AsReadOnly());

        _aiGeneratorMock.Setup(g => g.GenerateAssistanceNoteAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<SimilarTicketResult>>(),
                It.IsAny<IEnumerable<ResolutionProcedure>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("## ✅ Suggested Resolution\nChanger la dalle d'affichage.");

        // Configuration optionnelle du SaveChanges pour éviter tout comportement inattendu
        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var worker = new AIAssistanceWorker(_queueMock.Object, _serviceProvider, _loggerMock.Object);

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(150); // Temps nécessaire accordé au ThreadPool pour consommer le Channel
        await worker.StopAsync(CancellationToken.None);

        // Assert
        _aiGeneratorMock.Verify(g => g.GenerateAssistanceNoteAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<SimilarTicketResult>>(),
            It.IsAny<IEnumerable<ResolutionProcedure>>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}