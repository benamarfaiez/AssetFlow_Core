import { Routes } from '@angular/router';

/**
 * Routes de la fonctionnalité « tickets » (Lot 5, §5.B), chargées à la demande.
 *
 * `nouveau` est déclarée **avant** `:id` : un segment paramétré capturerait sinon le littéral
 * (`/tickets/nouveau` serait résolu comme `id === 'nouveau'`). `:id` alimente directement l'entrée
 * `id` de `Fiche`, et `assetId` (query param `?assetId=...`) l'entrée du même nom sur `Formulaire`,
 * via `withComponentInputBinding()` (voir `app.config.ts`) — les deux paramètres et les query
 * params sont liés par défaut, sans lecture manuelle d'`ActivatedRoute`.
 *
 * Aucun état partagé entre écrans ici (à la différence d'`InventaireService` côté `assets`) :
 * `GET /api/v1/tickets` est paginé/filtré/trié côté serveur, donc un incident nouvellement créé
 * n'a pas de position évidente dans une page déjà chargée — `Formulaire` navigue vers la fiche du
 * ticket créé plutôt que de tenter de mettre à jour la file de travail en mémoire.
 */
export const TICKETS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./file-de-travail/file-de-travail').then((m) => m.FileDeTravail),
    title: 'File de travail — AssetFlow Core',
  },
  {
    path: 'nouveau',
    loadComponent: () => import('./formulaire/formulaire').then((m) => m.Formulaire),
    title: 'Ouvrir un incident — AssetFlow Core',
  },
  {
    path: ':id',
    loadComponent: () => import('./fiche/fiche').then((m) => m.Fiche),
    title: "Fiche d'un incident — AssetFlow Core",
  },
];
