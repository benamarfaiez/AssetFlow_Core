import {
  BrowserCacheLocation,
  Configuration,
  LogLevel,
  type AccountInfo,
  type RedirectRequest,
  type SilentRequest,
} from '@azure/msal-browser';
import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';
import type { EntraIdConfig } from '../../../environments/environment.model';

/**
 * Paramètres Entra ID actifs, exposés en jeton d'injection plutôt que lus directement dans
 * `environment.entra` par chaque consommateur : le système de test unitaire du dépôt
 * (`@angular/build:unit-test`) interdit `vi.mock` sur les imports relatifs, et impose de passer
 * par `TestBed` pour toute substitution — ce jeton est le point que les tests substituent pour
 * simuler un tenant Entra ID configuré, sans jamais committer de vraie valeur ici.
 */
export const ENTRA_CONFIG = new InjectionToken<EntraIdConfig>('Configuration Entra ID', {
  providedIn: 'root',
  factory: () => environment.entra,
});

/**
 * Configuration MSAL (`@azure/msal-browser`), dérivée de {@link ENTRA_CONFIG}.
 *
 * Décision 0.1 : OIDC, Authorization Code + PKCE (flux par défaut de `PublicClientApplication`
 * dans un navigateur — aucune option à activer explicitement).
 *
 * N'est appelée que si `EntraAuthService` a constaté que `config.clientId` n'est pas vide : tant
 * que le tenant Entra ID (étape 7.0) n'existe pas, aucune instance MSAL n'est créée avec cette
 * configuration, ce qui évite de tenter de résoudre une autorité invalide.
 */
export function creerConfigurationMsal(config: EntraIdConfig): Configuration {
  return {
    auth: {
      clientId: config.clientId,
      authority: config.authority,
      redirectUri: config.redirectUri,
      postLogoutRedirectUri: config.redirectUri,
    },
    cache: {
      // Cache propre de MSAL (compte, jeton d'identité) : `sessionStorage`, jamais
      // `localStorage`. Le jeton d'accès applicatif suit une règle séparée et plus stricte —
      // mémoire uniquement, via `AuthTokenService` — sans lien avec ce cache.
      cacheLocation: BrowserCacheLocation.SessionStorage,
    },
    system: {
      loggerOptions: {
        // Aucun jeton, revendication ou en-tête ne doit jamais être journalisé — y compris en
        // développement. Le callback ignore délibérément tout message plutôt que de le relayer.
        loggerCallback: () => undefined,
        logLevel: LogLevel.Error,
        piiLoggingEnabled: false,
      },
    },
  };
}

/** Portées actives pour l'API : vide tant qu'`apiScope` n'est pas renseigné (étape 7.0). */
function porteesApi(config: EntraIdConfig): string[] {
  return config.apiScope === '' ? [] : [config.apiScope];
}

/**
 * Requête de connexion interactive (redirection complète, jamais de popup).
 *
 * @param destination Chemin interne à restaurer après connexion, porté par l'état `state` de la
 * requête OIDC — c'est ainsi qu'il survit à la redirection complète de page (tout état en
 * mémoire serait perdu). Validé côté appelant (`estCheminInterne`) avant d'être transmis ici.
 */
export function creerRequeteConnexion(
  config: EntraIdConfig,
  destination?: string,
): RedirectRequest {
  return {
    scopes: porteesApi(config),
    state: destination,
  };
}

/** Requête de renouvellement silencieux (`acquireTokenSilent`), pour un compte déjà connu. */
export function creerRequeteSilencieuse(config: EntraIdConfig, compte: AccountInfo): SilentRequest {
  return {
    scopes: porteesApi(config),
    account: compte,
  };
}
