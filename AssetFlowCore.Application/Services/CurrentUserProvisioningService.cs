using AssetFlowCore.Application.Interfaces;
using AssetFlowCore.Domain.Entities;
using AssetFlowCore.Domain.Repositories;

namespace AssetFlowCore.Application.Services;

/// <summary>
/// Ne persiste pas elle-même : <see cref="IUserRepository.AddAsync"/> ne fait que suivre l'entité
/// (même convention que les autres dépôts). C'est le <c>SaveChangesAsync</c> unique du handler
/// appelant qui persiste à la fois le nouvel utilisateur et la mutation du ticket, dans la même transaction.
/// </summary>
public class CurrentUserProvisioningService(
    IAuthenticatedUserAccessor authenticatedUserAccessor,
    IUserRepository userRepository) : ICurrentUserService
{
    public async Task<Guid> GetOrCreateUserIdAsync(CancellationToken cancellationToken = default)
    {
        var externalId = authenticatedUserAccessor.ExternalId;
        var existing = await userRepository.GetByExternalIdAsync(externalId, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var user = new User(Guid.NewGuid(), externalId, authenticatedUserAccessor.DisplayName, authenticatedUserAccessor.Email);
        await userRepository.AddAsync(user, cancellationToken);
        return user.Id;
    }
}
