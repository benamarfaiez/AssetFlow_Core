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
}
