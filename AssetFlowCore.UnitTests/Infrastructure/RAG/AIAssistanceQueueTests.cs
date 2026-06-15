using AssetFlowCore.Infrastructure.RAG.BackgroundQueue;
using FluentAssertions;

namespace AssetFlowCore.UnitTests.Infrastructure.RAG;

public class AIAssistanceQueueTests
{
    [Fact]
    public async Task QueueAndDequeue_ShouldFollowFifoOrder()
    {
        // Arrange
        var queue = new AIAssistanceQueue();
        var firstTicketId = Guid.NewGuid();
        var secondTicketId = Guid.NewGuid();

        // Act
        await queue.QueueTicketAsync(firstTicketId);
        await queue.QueueTicketAsync(secondTicketId);

        var result1 = await queue.DequeueTicketAsync(CancellationToken.None);
        var result2 = await queue.DequeueTicketAsync(CancellationToken.None);

        // Assert
        result1.Should().Be(firstTicketId);
        result2.Should().Be(secondTicketId);
    }
}