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
};
