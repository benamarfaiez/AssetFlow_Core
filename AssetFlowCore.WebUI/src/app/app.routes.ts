import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

/**
 * Routes racine, **entièrement chargées à la demande** (`loadChildren`).
 *
 * `diagnostic` et `design-system` (preuves d'exécution du socle et des Lots 3/4) ont été retirées
 * une fois les trois features du Lot 5 livrées, comme prévu dès leur création — voir
 * doc/IMPLEMENTATION-PLAN.md. La route d'accueil pointe désormais vers l'inventaire des actifs
 * (`E-01`), premier écran du parcours le plus courant.
 *
 * `authGuard` (`canMatch`, Lot 7 étape 7.6) est câblé sur chaque route de premier niveau : le
 * masquage d'actions selon le rôle reste, lui, à la charge de chaque écran (`JwtRolesService`).
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'assets' },
  {
    path: 'assets',
    canMatch: [authGuard],
    loadChildren: () => import('./features/assets/assets.routes').then((m) => m.ASSETS_ROUTES),
  },
  {
    path: 'tickets',
    canMatch: [authGuard],
    loadChildren: () => import('./features/tickets/tickets.routes').then((m) => m.TICKETS_ROUTES),
  },
  {
    path: 'teams',
    canMatch: [authGuard],
    loadChildren: () => import('./features/teams/teams.routes').then((m) => m.TEAMS_ROUTES),
  },
  // Repli provisoire : aucun écran dédié « page introuvable » n'existe encore.
  { path: '**', redirectTo: '' },
];
