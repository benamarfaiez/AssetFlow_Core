using Xunit;
using Moq;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using AssetFlowCore.Infrastructure.Notifications;
using AssetFlowCore.Application.DTOs;

namespace AssetFlowCore.UnitTests.Infrastructure.Notifications;

public class SignalRNotificationServiceTests
{
    [Fact]
    public async Task NotifyTeamNewTicketAsync_ShouldInvokeSendAsyncOnSignalRGroupClients()
    {
        // Arrange
        var hubContextMock = new Mock<IHubContext<TicketHub>>();
        var hubClientsMock = new Mock<IHubClients>();
        var clientProxyMock = new Mock<IClientProxy>();

        // hubContext.Clients -> Retourne le mock IHubClients
        hubContextMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);

        // hubClients.Group("NomDeLequipe") -> Retourne le mock de client proxy
        string targetTeam = "Support-VIP";
        hubClientsMock.Setup(c => c.Group(targetTeam)).Returns(clientProxyMock.Object);

        // Configuration de la méthode interne de SignalR (SendCoreAsync)
        clientProxyMock
            .Setup(p => p.SendCoreAsync(
                "ReceiveNewTicket",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new SignalRNotificationService(hubContextMock.Object);

        var dtoPayload = new TicketResponseDto(
            Id: Guid.NewGuid(),
            AssetId: Guid.NewGuid(),
            Title: "Écran cassé",
            Criticality: "High",
            Status: "Opened",
            AssignedTeamId: Guid.NewGuid(),
            AssignedTeamName: targetTeam
        );

        // Act
        Func<Task> act = async () => await service.NotifyTeamNewTicketAsync(targetTeam, dtoPayload);

        // Assert
        await act.Should().NotThrowAsync();

        // Vérification que le message est envoyé au bon groupe avec le bon payload
        clientProxyMock.Verify(
            p => p.SendCoreAsync(
                "ReceiveNewTicket",
                It.Is<object[]>(args => args.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}