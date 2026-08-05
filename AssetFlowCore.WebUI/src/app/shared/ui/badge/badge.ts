import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CLASSES_TONALITE, Tonalite } from '../tonalite';

/**
 * Badge d'état.
 *
 * Le libellé est **obligatoire** : c'est lui qui porte l'information. La tonalité et la pastille
 * ne font que la renforcer, ce qui satisfait WCAG 1.4.1 (l'information ne repose jamais sur la
 * seule couleur) et garde le badge lisible en niveaux de gris comme pour un daltonien.
 */
@Component({
  selector: 'app-badge',
  templateUrl: './badge.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex' },
})
export class Badge {
  /** Texte affiché, déjà traduit par l'appelant. */
  readonly libelle = input.required<string>();

  readonly tonalite = input<Tonalite>('neutre');

  /** Pastille de couleur devant le libellé. Purement décorative. */
  readonly avecPastille = input(true);

  protected readonly classes = computed(() =>
    [
      'inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-sm font-medium',
      CLASSES_TONALITE[this.tonalite()],
    ].join(' '),
  );
}
