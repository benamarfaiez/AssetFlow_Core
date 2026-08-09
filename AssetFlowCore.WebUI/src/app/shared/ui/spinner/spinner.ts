import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Taille de l'indicateur. `compact` s'insère dans un bouton, `normal` dans une zone de contenu. */
export type TailleSpinner = 'compact' | 'normal';

const CLASSES_TAILLE: Readonly<Record<TailleSpinner, string>> = {
  compact: 'size-4 border-2',
  normal: 'size-8 border-[3px]',
};

/**
 * Indicateur de chargement.
 *
 * Le libellé n'est jamais optionnel : la rotation seule n'annonce rien à un lecteur d'écran.
 * `role="status"` fait lire le libellé à l'apparition, et `libelleVisible` permet de le masquer
 * visuellement sans le retirer de l'arbre d'accessibilité (`sr-only`).
 *
 * L'animation est neutralisée sous `prefers-reduced-motion` : la remise à zéro globale de
 * `styles.css` s'en charge, et le libellé continue d'informer.
 */
@Component({
  selector: 'app-spinner',
  templateUrl: './spinner.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex items-center gap-2' },
})
export class Spinner {
  /** Ce que l'utilisateur attend, formulé pour être lu à voix haute. */
  readonly libelle = input($localize`:@@sharedUi.spinner.libelle:Chargement en cours`);

  readonly taille = input<TailleSpinner>('normal');

  /** À `false`, le libellé reste lu par les lecteurs d'écran mais n'occupe aucune place. */
  readonly libelleVisible = input(true);

  protected readonly classesCercle = computed(() =>
    [
      'animate-spin rounded-full border-bordure border-t-primaire',
      CLASSES_TAILLE[this.taille()],
    ].join(' '),
  );
}
