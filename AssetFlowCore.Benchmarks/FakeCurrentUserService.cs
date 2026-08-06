using AssetFlowCore.Application.Interfaces;

namespace AssetFlowCore.Benchmarks;

/// <summary>
/// Implémentation no-op de ICurrentUserService. Remplace le provisionnement JIT (HttpContext,
/// dépôt utilisateur) dans les benchmarks pour isoler la logique métier et la persistance mesurées.
/// </summary>
public sealed class FakeCurrentUserService : ICurrentUserService
{
    private static readonly Guid FixedUserId = Guid.Parse("00000000-0000-0000-0000-0000000000b1");

    public Task<Guid> GetOrCreateUserIdAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(FixedUserId);
}
