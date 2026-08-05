import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { routes } from './app.routes';
import { authTokenInterceptor } from './core/http/auth-token.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';

/**
 * Providers racine de l'application.
 *
 * **Détection de changement** : aucun provider n'est nécessaire. Le workspace a été créé en
 * mode *zoneless* — `zone.js` est absent des dépendances et n'est chargé par aucun polyfill —
 * ce qui est le comportement par défaut d'Angular 22. Conséquence à respecter dans tout le
 * code : un état modifié hors d'un signal ne déclenche aucun rendu.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),

    provideRouter(
      routes,
      // Lie les paramètres de route aux entrées du composant : une route `:id` alimente
      // directement un `input()`, sans lecture manuelle de l'`ActivatedRoute`.
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }),
    ),

    // `withFetch()` : client `fetch` plutôt que `XMLHttpRequest`.
    // Ordre des interceptors : le jeton est posé sur la requête sortante, puis les erreurs de
    // la réponse sont normalisées — la traduction en `ApiError` reste ainsi le dernier maillon.
    provideHttpClient(withFetch(), withInterceptors([authTokenInterceptor, errorInterceptor])),
  ],
};
