import { Routes } from '@angular/router';

/**
 * Route de la fonctionnalité « teams » (Lot 5, §5.C), chargée à la demande.
 *
 * Un seul écran (`E-07`), à la différence d'`assets`/`tickets` : liste, création, modification,
 * suppression et bascule d'activation y cohabitent (création/édition via une fenêtre modale,
 * pas une route dédiée — voir `teams.ts`).
 */
export const TEAMS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./teams').then((m) => m.Teams),
    title: $localize`:@@teams.routeTitre:Équipes — AssetFlow Core`,
  },
];
