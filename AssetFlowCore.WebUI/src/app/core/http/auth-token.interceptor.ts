import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthTokenService } from '../auth/auth-token.service';

/**
 * Attache le jeton d'accès aux requêtes destinées à l'API.
 *
 * ⚠️ **Inactif en l'état** : l'API n'expose aucune authentification (Lot 7, décision 0.1) et
 * `AuthTokenService` ne détient aucun jeton. L'interceptor laisse donc chaque requête
 * inchangée. Il est en place pour que l'activation du Lot 7 se limite à alimenter le service.
 *
 * Le jeton n'est joint qu'aux appels vers l'API : une requête sortant vers une autre origine
 * (ressource externe, télémétrie) ne doit jamais l'emporter.
 */
export const authTokenInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthTokenService).token();

  if (token === null || !isApiRequest(request.url)) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};

/**
 * Vrai si l'URL vise l'API AssetFlow. `apiBaseUrl` est vide en développement comme en
 * production (même origine, cf. `environment.ts`) : le test porte alors sur le chemin relatif.
 *
 * Exportée pour `sessionRenewalInterceptor`, qui doit ignorer les mêmes requêtes hors API —
 * évite de dupliquer ce test dans un second fichier.
 */
export function isApiRequest(url: string): boolean {
  const base = environment.apiBaseUrl;
  return base === '' ? url.startsWith('/api/') : url.startsWith(`${base}/api/`);
}
