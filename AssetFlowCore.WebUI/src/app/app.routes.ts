import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

/**
 * Routes racine, **entièrement chargées à la demande** (`loadChildren`) : aucune feature n'est
 * incluse dans le lot initial.
 *
 * Les routes `assets`, `tickets` et `teams` seront ajoutées au Lot 5, sur le même modèle. La
 * route d'accueil désigne pour l'instant l'écran de diagnostic du socle, à remplacer par
 * l'inventaire (`E-01`) dès qu'il existera.
 *
 * `authGuard` (`canMatch`, Lot 7 étape 7.6) est câblé sur les deux routes existantes à titre de
 * fondation : ni `diagnostic` ni `design-system` n'est un écran réservé à un rôle (aucun écran du
 * Lot 5 n'existe encore) — voir le compte-rendu de livraison pour ce que ce câblage rend actif
 * ou laisse inerte tant que le tenant Entra ID (étape 7.0) n'existe pas.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'diagnostic' },
  {
    path: 'diagnostic',
    canMatch: [authGuard],
    loadChildren: () =>
      import('./features/diagnostic/diagnostic.routes').then((m) => m.DIAGNOSTIC_ROUTES),
  },
  {
    path: 'design-system',
    canMatch: [authGuard],
    loadChildren: () =>
      import('./features/design-system/design-system.routes').then((m) => m.DESIGN_SYSTEM_ROUTES),
  },
  // Repli provisoire : l'écran « page introuvable » relève du Lot 4 (composants d'état).
  { path: '**', redirectTo: '' },
];
