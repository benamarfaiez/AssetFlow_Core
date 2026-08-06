using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Application.Services;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace AssetFlowCore.UnitTests.Application.Services;

public class CurrentUserProvisioningServiceTests
{
    private readonly Mock<IAuthenticatedUserAccessor> _userAccessorMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly CurrentUserProvisioningService _service;

    public CurrentUserProvisioningServiceTests() => _service = new CurrentUserProvisioningService(_userAccessorMock.Object, _userRepoMock.Object);

    [Fact]
    public async Task GetOrCreateUserIdAsync_WhenUserAlreadyExists_ShouldReturnExistingId_AndNotAddAgain()
    {
        var existing = new User(Guid.NewGuid(), "oid-connu", "Alice", "alice@example.com");
        _userAccessorMock.Setup(a => a.ExternalId).Returns("oid-connu");
        _userRepoMock.Setup(r => r.GetByExternalIdAsync("oid-connu", It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var userId = await _service.GetOrCreateUserIdAsync(CancellationToken.None);

        userId.Should().Be(existing.Id);
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateUserIdAsync_WhenUserUnknown_ShouldProvisionJustInTime()
    {
        _userAccessorMock.Setup(a => a.ExternalId).Returns("oid-nouveau");
        _userAccessorMock.Setup(a => a.DisplayName).Returns("Bob");
        _userAccessorMock.Setup(a => a.Email).Returns("bob@example.com");
        _userRepoMock.Setup(r => r.GetByExternalIdAsync("oid-nouveau", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        User? added = null;
        _userRepoMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => added = u)
            .Returns(Task.CompletedTask);

        var userId = await _service.GetOrCreateUserIdAsync(CancellationToken.None);

        added.Should().NotBeNull();
        added!.ExternalId.Should().Be("oid-nouveau");
        added.DisplayName.Should().Be("Bob");
        userId.Should().Be(added.Id);
    }
}
