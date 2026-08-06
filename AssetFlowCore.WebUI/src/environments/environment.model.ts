/**
 * Paramètres Microsoft Entra ID (décision 0.1 : OIDC, Authorization Code + PKCE via
 * `@azure/msal-browser`). Valeurs **placeholder vides** dans les deux fichiers `environment*.ts`
 * tant que le tenant réel n'existe pas (Lot 7, étape 7.0, opérationnelle — pas du code) : même
 * convention que le backend (`Authentication:Entra:Authority`/`Audience` vides), on ne commite
 * jamais de vraie valeur.
 */
export interface EntraIdConfig {
  /** Autorité OIDC, ex. `https://login.microsoftonline.com/<tenantId>`. Vide : rien n'est configuré. */
  readonly authority: string;

  /** Identifiant de l'application enregistrée en plateforme SPA dans Entra ID. */
  readonly clientId: string;

  /** URI de redirection après connexion, enregistrée côté Entra ID (plateforme SPA). */
  readonly redirectUri: string;

  /**
   * Portée demandée pour appeler l'API (ex. `api://<clientId>/access_as_user`) — pas l'audience
   * brute : c'est le jeton d'accès ainsi obtenu que `Authentication:Entra:Audience` doit valider
   * côté API.
   */
  readonly apiScope: string;
}

/**
 * Forme de la configuration d'environnement. Les deux fichiers `environment*.ts` s'y
 * conforment, ce qui interdit qu'une clé existe dans un environnement et pas dans l'autre —
 * le remplacement de fichier opéré par `angular.json` ne le signalerait pas autrement.
 */
export interface EnvironmentConfig {
  /** Vrai pour les builds destinés à la production. */
  readonly production: boolean;

  /**
   * Racine de l'API, sans barre oblique finale. Une chaîne vide signifie « même origine » :
   * les requêtes partent en relatif (`/api/assets`). C'est le seul endroit à modifier pour
   * viser une API hébergée sur une autre origine.
   */
  readonly apiBaseUrl: string;

  /** Adresse du hub SignalR des incidents, relative à l'origine si `apiBaseUrl` est vide. */
  readonly ticketHubUrl: string;

  /** Paramètres Microsoft Entra ID — voir {@link EntraIdConfig}. */
  readonly entra: EntraIdConfig;
}
