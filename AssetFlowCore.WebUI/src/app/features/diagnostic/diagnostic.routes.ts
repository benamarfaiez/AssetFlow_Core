import { Routes } from '@angular/router';

/** Routes de l'écran de diagnostic du socle, chargées à la demande. */
export const DIAGNOSTIC_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./diagnostic').then((m) => m.Diagnostic),
    title: 'Diagnostic du socle — AssetFlow Core',
  },
];
