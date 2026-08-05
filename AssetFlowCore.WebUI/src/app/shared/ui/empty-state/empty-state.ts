import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Message d'absence de contenu — l'un des quatre états que chaque écran doit distinguer
 * (chargement, vide, erreur, contenu).
 *
 * Aucun titre de niveau `h*` n'est imposé : le composant ignore sa place dans la hiérarchie de
 * l'écran, et un niveau arbitraire y créerait un saut. L'action éventuelle est projetée par
 * l'appelant, qui seul sait ce qu'il faut proposer.
 */
@Component({
  selector: 'app-empty-state',
  templateUrl: './empty-state.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyState {
  /** Constat, formulé positivement : « Aucun incident en cours ». */
  readonly titre = input.required<string>();

  /** Précision facultative : pourquoi c'est vide, ou quoi faire ensuite. */
  readonly description = input<string | null>(null);
}
