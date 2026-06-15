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

    [Fact]
    public async Task GenerateAssistanceNoteAsync_ShouldReturnModelOutput_WhenCalledWithValidData()
    {
        // Arrange
        var description = "Le serveur de base de données est surchargé.";
        var expectedResponse = "## 🔍 Diagnostic Steps\n1. Vérifier les index.";

        // On crée le contenu de réponse textuel
        var chatContent = new ChatMessageContent(AuthorRole.Assistant, expectedResponse);

        // La vraie méthode d'interface retourne une liste en lecture seule (IReadOnlyList)
        IReadOnlyList<ChatMessageContent> mockResponseList = new List<ChatMessageContent> { chatContent }.AsReadOnly();

        // Configurer la VRAIE méthode d'interface (avec ses 4 arguments précis)
        _chatMock.Setup(c => c.GetChatMessageContentsAsync(
            It.IsAny<ChatHistory>(),
            It.IsAny<PromptExecutionSettings>(),
            It.IsAny<Kernel>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponseList);

        // Act
        var result = await _generator.GenerateAssistanceNoteAsync(
            description,
            Enumerable.Empty<SimilarTicketResult>(),
            Enumerable.Empty<ResolutionProcedure>());

        // Assert
        result.Should().Be(expectedResponse);

        // Adapter également la vérification (Verify) sur la bonne méthode d'interface
        _chatMock.Verify(c => c.GetChatMessageContentsAsync(
            It.IsAny<ChatHistory>(),
            It.IsAny<PromptExecutionSettings>(),
            It.IsAny<Kernel>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAssistanceNoteAsync_WithEmptyDescription_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _generator.GenerateAssistanceNoteAsync("", Enumerable.Empty<SimilarTicketResult>(), Enumerable.Empty<ResolutionProcedure>()));
    }
}
