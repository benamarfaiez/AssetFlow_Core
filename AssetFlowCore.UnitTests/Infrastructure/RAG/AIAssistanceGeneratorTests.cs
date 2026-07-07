using AssetFlowCore.Application.Models.RAG;
using AssetFlowCore.Infrastructure.RAG;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;

namespace AssetFlowCore.UnitTests.Infrastructure.RAG;

public class AIAssistanceGeneratorTests
{
    private readonly Mock<IChatCompletionService> _chatMock = new();
    private readonly Mock<ILogger<AIAssistanceGenerator>> _loggerMock = new();
    private readonly AIAssistanceGenerator _generator;

    public AIAssistanceGeneratorTests()
    {
        _generator = new AIAssistanceGenerator(_chatMock.Object, _loggerMock.Object);
    }

    private void SetupMockChatResponse(string? content, Dictionary<string, object?>? metadata = null)
    {
        var chatContent = new ChatMessageContent(AuthorRole.Assistant, content)
        {
            Metadata = metadata
        };

        IReadOnlyList<ChatMessageContent> mockResponseList = new List<ChatMessageContent> { chatContent }.AsReadOnly();

        // IChatCompletionService utilise intrinsèquement GetChatMessageContentsAsync sous le capot des méthodes d'extension
        _chatMock.Setup(c => c.GetChatMessageContentsAsync(
            It.IsAny<ChatHistory>(),
            It.IsAny<PromptExecutionSettings>(),
            It.IsAny<Kernel>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponseList);
    }

    #region Tests pour GenerateAssistanceNoteAsync

    [Fact]
    public async Task GenerateAssistanceNoteAsync_ShouldReturnContentAndLogUsage_WhenMetadataIsPresent()
    {
        // Arrange
        var description = "Le serveur de base de données est surchargé.";
        var expectedResponse = "## 🔍 Diagnostic Steps\n1. Vérifier les index.";
        var mockMetadata = new Dictionary<string, object?> { { "Usage", 42 } };

        SetupMockChatResponse(expectedResponse, mockMetadata);

        var similarTickets = new List<SimilarTicketResult>
        {
            new(Guid.NewGuid().ToString(), "Ticket passé", "Résolution passée", 0.95f)
        };
        var suggestedProcedures = new List<ResolutionProcedure>
        {
            new(
                "PROC-001",                           // 1. ProcedureId (string)
                "Procédure d'optimisation d'index",    // 2. Title (string)
                ["Étape 1", "Étape 2"], // 3. Steps (IReadOnlyList<string>)
                0.88f                                 // 4. RelevanceScore (float)
            )
        };

        // Act
        var result = await _generator.GenerateAssistanceNoteAsync(description, similarTickets, suggestedProcedures);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task GenerateAssistanceNoteAsync_ShouldReturnEmptyString_WhenModelReturnsNullContent()
    {
        // Arrange
        SetupMockChatResponse(null); // Simule une réponse vide de LLM

        // Act
        var result = await _generator.GenerateAssistanceNoteAsync("Description", [], []);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GenerateAssistanceNoteAsync_WithInvalidDescription_ShouldThrowArgumentException(string? invalidDesc)
    {
        // Act & Assert
        await _generator.Invoking(g => g.GenerateAssistanceNoteAsync(invalidDesc!, [], []))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GenerateAssistanceNoteAsync_ShouldLogErrorAndRethrow_WhenChatServiceFails()
    {
        // Arrange
        _chatMock.Setup(c => c.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), It.IsAny<PromptExecutionSettings>(), It.IsAny<Kernel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Ollama unreachable"));

        // Act & Assert
        await _generator.Invoking(g => g.GenerateAssistanceNoteAsync("Panne totale", [], []))
            .Should().ThrowAsync<InvalidOperationException>();

        // Vérifie qu'un log d'erreur a bien été écrit (Couverture du bloc catch)
        _loggerMock.Verify(
        l => l.Log<It.IsAnyType>(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Échec de la génération de la note d'assistance IA")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
        Times.Once);
    }

    #endregion

    #region Tests pour GenerateResolutionSummaryAsync

    [Fact]
    public async Task GenerateResolutionSummaryAsync_ShouldReturnSummary_WhenCalledWithValidData()
    {
        // Arrange
        var description = "Écran noir sur PC Portable.";
        var resolution = "Remplacement du câble de la dalle.";
        var expectedSummary = "## Incident Summary\nProblème d'affichage résolu.";

        SetupMockChatResponse(expectedSummary);

        // Act
        var result = await _generator.GenerateResolutionSummaryAsync(description, resolution);

        // Assert
        result.Should().Be(expectedSummary);
    }

    [Theory]
    [InlineData("", "Une résolution")]
    [InlineData("Une description", "")]
    [InlineData(null, "Une résolution")]
    [InlineData("Une description", null)]
    public async Task GenerateResolutionSummaryAsync_WithInvalidArguments_ShouldThrowArgumentException(string? desc, string? res)
    {
        // Act & Assert
        await _generator.Invoking(g => g.GenerateResolutionSummaryAsync(desc!, res!))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GenerateResolutionSummaryAsync_ShouldLogErrorAndRethrow_WhenChatServiceFails()
    {
        // Arrange
        _chatMock.Setup(c => c.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), It.IsAny<PromptExecutionSettings>(), It.IsAny<Kernel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KernelException("LLM Timeout"));

        // Act & Assert
        await _generator.Invoking(g => g.GenerateResolutionSummaryAsync("Panne matérielle", "Réparation"))
            .Should().ThrowAsync<KernelException>();

        // Vérifie la couverture du bloc catch de GenerateResolutionSummaryAsync
        _loggerMock.Verify(
        l => l.Log<It.IsAnyType>(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Échec de la génération du résumé de résolution")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
        Times.Once);
    }

    #endregion
}