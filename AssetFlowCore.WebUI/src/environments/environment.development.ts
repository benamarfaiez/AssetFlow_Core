import { EnvironmentConfig } from './environment.model';

/**
 * Environnement de développement, substitué au précédent par `fileReplacements` (angular.json).
 *
 * `apiBaseUrl` reste vide : les requêtes partent en relatif et `proxy.conf.json` les
 * réexpédie vers `https://localhost:7138`. Ce détour évite à la fois le CORS et le refus
 * du certificat de développement de l'API, que le navigateur opposerait à un appel direct.
 */
export const environment: EnvironmentConfig = {
  production: false,
  apiBaseUrl: '',
  ticketHubUrl: '/ticketHub',
};
