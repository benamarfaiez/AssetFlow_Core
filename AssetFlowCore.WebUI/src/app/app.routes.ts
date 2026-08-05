import { Routes } from '@angular/router';

/**
 * Routes racine, **entièrement chargées à la demande** (`loadChildren`) : aucune feature n'est
 * incluse dans le lot initial.
 *
 * Les routes `assets`, `tickets` et `teams` seront ajoutées au Lot 5, sur le même modèle. La
 * route d'accueil désigne pour l'instant l'écran de diagnostic du socle, à remplacer par
 * l'inventaire (`E-01`) dès qu'il existera.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'diagnostic' },
  {
    path: 'diagnostic',
    loadChildren: () =>
      import('./features/diagnostic/diagnostic.routes').then((m) => m.DIAGNOSTIC_ROUTES),
  },
  {
    path: 'design-system',
    loadChildren: () =>
      import('./features/design-system/design-system.routes').then((m) => m.DESIGN_SYSTEM_ROUTES),
  },
  // Repli provisoire : l'écran « page introuvable » relève du Lot 4 (composants d'état).
  { path: '**', redirectTo: '' },
];
