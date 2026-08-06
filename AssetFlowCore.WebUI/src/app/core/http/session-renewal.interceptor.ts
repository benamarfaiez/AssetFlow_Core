import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { EntraAuthService } from '../auth/entra-auth.service';
import { isApiRequest } from './auth-token.interceptor';

/**
 * Rejoue **une seule fois** une requête d'API ayant échoué en 401, après un renouvellement
 * silencieux du jeton (`EntraAuthService.renouvelerJeton`). Un 403 n'est en revanche jamais
 * rejoué : ce n'est pas un jeton périmé qui est en cause, mais une autorisation refusée que le
 * renouvellement ne changerait pas.
 *
 * **Placement dans `app.config.ts`** : enregistré **après** `errorInterceptor`. La réponse
 * remonte la chaîne des intercepteurs dans l'ordre **inverse** de la liste passée à
 * `withInterceptors` — celui-ci, plus proche du transport, voit donc la `HttpErrorResponse`
 * brute avant qu'`errorInterceptor` ne la traduise en `ApiError`. Une erreur déjà traduite
 * (`ApiError`) ne peut donc pas apparaître ici : seul le code de statut brut est inspecté.
 *
 * Ne relit jamais `ProblemDetails` : la classification métier reste entièrement dans
 * `errorInterceptor`, seul point qui l'interprète.
 */
export const sessionRenewalInterceptor: HttpInterceptorFn = (request, next) => {
  const authentification = inject(EntraAuthService);

  if (!isApiRequest(request.url)) {
    return next(request);
  }

  return next(request).pipe(
    catchError((erreur: unknown) => {
      if (!(erreur instanceof HttpErrorResponse) || erreur.status !== 401) {
        return throwError(() => erreur);
      }

      return from(authentification.renouvelerJeton()).pipe(
        switchMap((jeton) => {
          if (jeton === null) {
            // Renouvellement impossible (pas de compte actif, session révoquée) : l'échec
            // d'origine remonte inchangé, à traduire par `errorInterceptor`.
            return throwError(() => erreur);
          }

          // `authTokenInterceptor` a déjà agi sur la requête d'origine, en amont : il ne sera
          // pas rappelé pour ce rejeu, l'en-tête est donc reposé ici avec le jeton frais.
          return next(request.clone({ setHeaders: { Authorization: `Bearer ${jeton}` } }));
        }),
      );
    }),
  );
};
