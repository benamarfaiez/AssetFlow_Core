import { Routes } from '@angular/router';
import { InventaireService } from './inventaire/inventaire.service';

/**
 * Routes de la fonctionnalité « assets » (Lot 5, §5.A), chargées à la demande.
 *
 * Nichées sous une route parente `path: ''` sans composant, dont le seul rôle est de porter
 * `providers: [InventaireService]` : le routeur crée un `EnvironmentInjector` dédié à ce nœud et à
 * ses enfants (`Route.providers`, `@angular/router` 22.1.0), partagé par les trois écrans sans
 * survivre au-delà de `/assets/**` — voir le commentaire de tête d'`InventaireService` pour le
 * motif (partage entre `E-01`/`E-02`, requis par le critère P-01, sans les inconvénients d'un
 * `providedIn: 'root'`). Le nid ne change aucune URL : segment vide, mêmes chemins enfants.
 *
 * `nouveau` est déclarée **avant** `:id` : un segment paramétré capturerait sinon le littéral
 * (`/assets/nouveau` serait résolu comme `id === 'nouveau'`). `:id` alimente directement l'entrée
 * `id` de `Fiche` via `withComponentInputBinding()` (voir `app.config.ts`), sans lecture manuelle
 * de `ActivatedRoute`.
 */
export const ASSETS_ROUTES: Routes = [
  {
    path: '',
    providers: [InventaireService],
    children: [
      {
        path: '',
        loadComponent: () => import('./inventaire/inventaire').then((m) => m.Inventaire),
        title: 'Inventaire des actifs — AssetFlow Core',
      },
      {
        path: 'nouveau',
        loadComponent: () => import('./formulaire/formulaire').then((m) => m.Formulaire),
        title: 'Enregistrer un actif — AssetFlow Core',
      },
      {
        path: ':id',
        loadComponent: () => import('./fiche/fiche').then((m) => m.Fiche),
        title: "Fiche d'un actif — AssetFlow Core",
      },
    ],
  },
];
