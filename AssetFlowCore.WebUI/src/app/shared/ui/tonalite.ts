/**
 * Tonalité sémantique partagée par les composants d'état (badge, message, notification).
 *
 * Elle ne désigne jamais une couleur mais une **intention** : les jetons correspondants
 * (`--color-<tonalite>-fond` / `--color-<tonalite>-contenu`) changent avec le thème, et chaque
 * composant ajoute toujours un libellé — l'information ne repose jamais sur la seule couleur.
 */
export type Tonalite = 'neutre' | 'info' | 'succes' | 'alerte' | 'danger';

/**
 * Classes utilitaires par tonalité. Écrites en clair, sans concaténation dynamique : Tailwind
 * détecte les classes en analysant les sources, et ne verrait pas `bg-${tonalite}-fond`.
 */
export const CLASSES_TONALITE: Readonly<Record<Tonalite, string>> = {
  neutre: 'bg-neutre-fond text-neutre-contenu',
  info: 'bg-info-fond text-info-contenu',
  succes: 'bg-succes-fond text-succes-contenu',
  alerte: 'bg-alerte-fond text-alerte-contenu',
  danger: 'bg-danger-fond text-danger-contenu',
};
