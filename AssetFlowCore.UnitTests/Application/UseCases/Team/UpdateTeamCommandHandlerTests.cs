using AssetFlowCore.Application.UseCases.Team.UpdateTeam;
using AssetFlowCore.Domain.Exceptions;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace AssetFlowCore.UnitTests.Application.UseCases.Team;

public class UpdateTeamCommandHandlerTests
{
    private readonly Mock<ITeamRepository> _teamRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly UpdateTeamCommandHandler _handler;

    public UpdateTeamCommandHandlerTests()
    {
        // On lie les sous-propri�t�s de l'Unit of Work � nos mocks de repositories
        _uow.Setup(u => u.Team).Returns(_teamRepo.Object);
        _handler = new UpdateTeamCommandHandler(_uow.Object);
    }

    [Fact]
    public async Task HandleAsync_WithExistingTeam_ShouldUpdateFieldsAndSave()
    {
        // Arrange
        var team = new AssetFlowCore.Domain.Entities.Team("OldName", "Server", "High", "OldDesc");
        _teamRepo.Setup(r => r.GetByIdAsync(team.Id, CancellationToken.None)).ReturnsAsync(team);

        var command = new UpdateTeamCommand(team.Id, "NewName", "Laptop", "Low", "NewDesc");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("NewName");
        result.Id.Should().Be(team.Id);
        team.AssetType.Should().Be("Laptop");
        team.TicketCriticality.Should().Be("Low");
        team.Description.Should().Be("NewDesc");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTeamNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _teamRepo.Setup(r => r.GetByIdAsync(id, CancellationToken.None)).ReturnsAsync((AssetFlowCore.Domain.Entities.Team?)null);
        var command = new UpdateTeamCommand(id, "Name", "Desc", "Server", "High");

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>().WithMessage($"L'équipe {id} est introuvable.");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
