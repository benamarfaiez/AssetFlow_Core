import { EnvironmentConfig } from './environment.model';

/**
 * Environnement par défaut, utilisé par les builds de production.
 *
 * `apiBaseUrl` est vide volontairement : hors Development, l'API .NET n'applique **aucune**
 * politique CORS (`Program.cs` ne branche `UseCors` qu'en Development). Le frontend et l'API
 * doivent donc être servis sur la **même origine**, derrière un reverse proxy — un appel
 * inter-origines serait rejeté par le navigateur.
 */
export const environment: EnvironmentConfig = {
  production: true,
  apiBaseUrl: '',
  ticketHubUrl: '/ticketHub',
  // Placeholders vides : le tenant Entra ID (étape 7.0) n'existe pas encore. Tant que
  // `clientId` est vide, `EntraAuthService` ne construit aucune instance MSAL — l'authentification
  // reste inerte plutôt que de tenter une connexion vers une autorité invalide.
  entra: {
    authority: '',
    clientId: '',
    redirectUri: '',
    apiScope: '',
  },
};
