import { Routes } from '@angular/router';

/** Routes de la page de revue du design system, chargée à la demande. */
export const DESIGN_SYSTEM_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./design-system').then((m) => m.DesignSystem),
    title: 'Design system — AssetFlow Core',
  },
];
