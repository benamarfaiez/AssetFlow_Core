import { inject } from '@angular/core';
import { Router, type CanMatchFn, type UrlSegment, type UrlTree } from '@angular/router';
import { AuthTokenService } from '../auth/auth-token.service';
import { EntraAuthService } from '../auth/entra-auth.service';

/**
 * Garde d'accès aux routes protégées.
 *
 * `canMatch`, jamais `canActivate` : toutes les routes de l'application sont en `loadChildren`
 * (`app.routes.ts`) — `canMatch` évite de télécharger le module d'un lot que l'utilisateur n'a
 * pas le droit d'ouvrir, alors que `canActivate` l'aurait déjà chargé avant de refuser d'y entrer.
 *
 * Attend l'initialisation de l'authentification (`EntraAuthService.pretAuthentification`) avant
 * de statuer : sans cette attente, un simple F5 rejetterait systématiquement l'utilisateur
 * pendant les quelques millisecondes que prend la connexion silencieuse (`acquireTokenSilent`)
 * au démarrage de l'application.
 *
 * Ne renvoie jamais un `false` nu : un refus produit toujours une `UrlTree`. La destination
 * demandée est mémorisée (état `state` de la requête OIDC, côté `EntraAuthService`) pour y
 * revenir après connexion — construite depuis les `UrlSegment` de la route, jamais depuis une
 * chaîne externe, ce qui exclut par construction toute redirection ouverte.
 *
 * Câblage de fondation, pas de protection réelle aujourd'hui : aucune des routes de premier
 * niveau (`assets`, `tickets`, `teams`) n'est réservée à un rôle particulier — voir le
 * compte-rendu de livraison pour ce qui est actif ou inerte tant que le tenant Entra ID
 * (étape 7.0) n'existe pas.
 */
export const authGuard: CanMatchFn = async (_route, segments): Promise<boolean | UrlTree> => {
  const jetons = inject(AuthTokenService);
  const authentification = inject(EntraAuthService);
  const router = inject(Router);

  await authentification.pretAuthentification;

  if (jetons.isAuthenticated()) {
    return true;
  }

  if (!authentification.estConfigure) {
    // Étape 7.0 non réalisée : aucun tenant Entra ID n'existe, donc aucune connexion n'est
    // possible. Refuser ici rendrait l'application totalement inaccessible avant cette étape
    // opérationnelle (et provoquerait une boucle de redirection sur '/') : la garde laisse
    // passer en attendant, faute de quoi il n'y aurait plus rien à protéger ni à câbler.
    return true;
  }

  const destination = cheminDemande(segments);

  // Navigation de page complète hors SPA : ne bloque pas le retour de la garde, qui doit malgré
  // tout produire une valeur concrète (cf. doc ci-dessus).
  void authentification.connecterEtRevenirA(destination);

  return router.parseUrl('/');
};

/**
 * Reconstruit le chemin demandé depuis les segments non consommés de la route — toujours un
 * chemin interne par construction (jamais une chaîne fournie par l'extérieur).
 */
function cheminDemande(segments: readonly UrlSegment[]): string {
  return `/${segments.map((segment) => segment.path).join('/')}`;
}
