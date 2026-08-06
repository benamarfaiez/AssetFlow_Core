namespace AssetFlowCore.Application.Interfaces;

/// <summary>
/// Résout l'identifiant local (<c>User.Id</c>) de l'utilisateur authentifié courant, en le
/// provisionnant « just-in-time » (décision 0.2, Lot 7) s'il s'agit de sa première requête.
/// </summary>
public interface ICurrentUserService
{
    Task<Guid> GetOrCreateUserIdAsync(CancellationToken cancellationToken = default);
}
