import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
} from '@angular/core';
import { Spinner } from '../spinner/spinner';

/** Intention de l'action portée par le bouton. */
export type VarianteBouton = 'primaire' | 'secondaire' | 'discret' | 'danger';

/** Taille du bouton. `compact` reste au-dessus de la cible tactile minimale. */
export type TailleBouton = 'compact' | 'normal';

const CLASSES_VARIANTE: Readonly<Record<VarianteBouton, string>> = {
  primaire: 'bg-primaire text-texte-inverse hover:bg-primaire-survol',
  secondaire: 'border border-bordure-controle bg-surface text-texte hover:bg-surface-creuse',
  discret: 'text-primaire hover:bg-surface-creuse',
  danger: 'bg-danger text-texte-inverse hover:bg-danger-survol',
};

const CLASSES_TAILLE: Readonly<Record<TailleBouton, string>> = {
  compact: 'px-2.5 py-1.5 text-sm',
  normal: 'px-4 py-2 text-sm',
};

/**
 * Bouton du design system.
 *
 * Rend un `<button>` **natif** : le type, l'appartenance au formulaire, l'état désactivé et la
 * gestion clavier restent ceux du navigateur. Les clics remontent naturellement au parent, qui
 * écoute donc `(click)` sur `<app-button>` sans qu'une sortie soit nécessaire.
 */
@Component({
  selector: 'app-button',
  imports: [Spinner],
  templateUrl: './button.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex' },
})
export class Button {
  /** Type HTML. `submit` uniquement pour le bouton principal d'un formulaire. */
  readonly type = input<'button' | 'submit' | 'reset'>('button');

  readonly variante = input<VarianteBouton>('primaire');

  readonly taille = input<TailleBouton>('normal');

  readonly disabled = input(false, { transform: booleanAttribute });

  /**
   * Action longue en cours : le bouton est inactif et annonce son état par `aria-busy`.
   * C'est ce qui empêche une double soumission pendant un appel réseau.
   */
  readonly enCours = input(false, { transform: booleanAttribute });

  /** Occupe toute la largeur disponible — utile sur petit écran. */
  readonly pleineLargeur = input(false, { transform: booleanAttribute });

  /**
   * Nom accessible, obligatoire lorsque le contenu projeté est une icône seule.
   * Laisser `null` quand le libellé visible suffit.
   */
  readonly ariaLabel = input<string | null>(null);

  /** Vrai si le bouton refuse l'interaction, pour l'une ou l'autre raison. */
  protected readonly inactif = computed(() => this.disabled() || this.enCours());

  protected readonly classes = computed(() =>
    [
      'inline-flex w-full items-center justify-center gap-2 rounded-md font-medium',
      'min-h-(--cible-tactile) transition-colors duration-(--duree-rapide)',
      'disabled:cursor-not-allowed disabled:opacity-60',
      CLASSES_VARIANTE[this.variante()],
      CLASSES_TAILLE[this.taille()],
      this.pleineLargeur() ? '' : 'sm:w-auto',
    ].join(' '),
  );
}
