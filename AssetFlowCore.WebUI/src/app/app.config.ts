import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import localeFr from '@angular/common/locales/fr';
import {
  ApplicationConfig,
  LOCALE_ID,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { routes } from './app.routes';
import { EntraAuthService } from './core/auth/entra-auth.service';
import { authTokenInterceptor } from './core/http/auth-token.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';
import { sessionRenewalInterceptor } from './core/http/session-renewal.interceptor';

// Nécessaire à tout pipe `date`/`number`/`currency` : sans elle, Angular retombe sur son
// locale par défaut (`en-US`) et un format de date anglais apparaîtrait au premier écran du
// Lot 5 qui en affiche une (historique de transfert, ouverture d'incident). `@angular/common`
// n'embarque que les données de la locale source par défaut ; l'enregistrer explicitement est
// requis même en l'absence de toute autre locale compilée.
registerLocaleData(localeFr);

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

    // `fr-FR`, alors que `localeFr` provient du module générique `@angular/common/locales/fr` :
    // Angular retombe automatiquement sur les données de la locale de base (`fr`) lorsqu'aucune
    // variante régionale (`fr-FR`) n'est enregistrée séparément — comportement documenté de
    // `findLocaleData`, pas une approximation de notre part.
    { provide: LOCALE_ID, useValue: 'fr-FR' },

    // Traite un éventuel retour de redirection Entra ID et tente une connexion silencieuse
    // (compte déjà en cache MSAL) **avant** que le routeur ne s'exécute — sans quoi la garde de
    // route de ce lot verrait systématiquement un utilisateur anonyme au premier rendu.
    provideAppInitializer(() => inject(EntraAuthService).initialiser()),

    provideRouter(
      routes,
      // Lie les paramètres de route aux entrées du composant : une route `:id` alimente
      // directement un `input()`, sans lecture manuelle de l'`ActivatedRoute`.
      withComponentInputBinding(),
      withInMemoryScrolling({ scrollPositionRestoration: 'enabled', anchorScrolling: 'enabled' }),
    ),

    // `withFetch()` : client `fetch` plutôt que `XMLHttpRequest`.
    //
    // Ordre des interceptors, complété au Lot 7 (étape 7.5 bis) — `sessionRenewalInterceptor`
    // s'ajoute **après** `errorInterceptor`, alors que ce dernier se présentait jusqu'ici comme
    // « le dernier maillon ». La réponse remonte la chaîne dans l'ordre **inverse** de cette
    // liste : `sessionRenewalInterceptor`, désormais le plus proche du transport, voit donc la
    // `HttpErrorResponse` brute d'un 401 avant qu'`errorInterceptor` ne la traduise en
    // `ApiError`, ce qui lui permet de rejouer la requête une fois après renouvellement du
    // jeton. `errorInterceptor` reste le seul point qui interprète `ProblemDetails`.
    provideHttpClient(
      withFetch(),
      withInterceptors([authTokenInterceptor, errorInterceptor, sessionRenewalInterceptor]),
    ),
  ],
};
