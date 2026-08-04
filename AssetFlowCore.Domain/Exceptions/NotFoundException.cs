namespace AssetFlowCore.Domain.Exceptions;

/// <summary>
/// Ressource désignée par l'URI d'une requête et absente du référentiel : traduite en
/// <b>404</b> par le middleware. Une référence invalide portée par le <i>corps</i> d'une
/// requête reste une <see cref="DomainException"/> (400) : la requête est recevable,
/// c'est la donnée fournie qui est refusée.
/// </summary>
public class NotFoundException(string message) : DomainException(message)
{
    /// <summary>Message normalisé « &lt;ressource&gt; &lt;identifiant&gt; est introuvable. »</summary>
    public static NotFoundException For(string resource, Guid id)
        => new($"{resource} {id} est introuvable.");
}
