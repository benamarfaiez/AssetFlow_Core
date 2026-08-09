import { CdkTrapFocus } from '@angular/cdk/a11y';
import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  effect,
  inject,
  input,
  output,
} from '@angular/core';

/** Compteur d'instances : le dialogue référence son titre par identifiant. */
let compteur = 0;

/**
 * Fenêtre modale.
 *
 * Accessibilité, dans l'ordre où elle se joue :
 * - `role="dialog"` + `aria-modal="true"` + `aria-labelledby` sur le titre : le dialogue est
 *   annoncé avec son intitulé ;
 * - `cdkTrapFocus` avec `autoCapture` : le focus entre dans le dialogue à l'ouverture, y reste
 *   pendant l'affichage, et **revient à l'élément déclencheur** à la fermeture — cette
 *   restitution est assurée par la destruction de la directive, donc par le retrait du bloc du
 *   DOM (`@if`), et non par un traitement de notre part ;
 * - `Échap` demande la fermeture, comme le clic sur l'arrière-plan si l'appelant l'autorise.
 *
 * Le composant ne décide **pas** de sa fermeture : il émet une demande et l'appelant remet
 * `ouverte` à `false`. C'est ce qui permet de refuser la fermeture d'un formulaire non
 * enregistré, par exemple.
 */
@Component({
  selector: 'app-modal',
  imports: [CdkTrapFocus],
  templateUrl: './modal.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Modal {
  private readonly document = inject(DOCUMENT);

  readonly ouverte = input.required<boolean>();

  /** Intitulé du dialogue, rendu en `h2` et référencé par `aria-labelledby`. */
  readonly titre = input.required<string>();

  /** Précision facultative, référencée par `aria-describedby`. */
  readonly description = input<string | null>(null);

  readonly taille = input<'normal' | 'large'>('normal');

  /** À `false`, seul `Échap` ou un bouton explicite peut demander la fermeture. */
  readonly fermetureParArrierePlan = input(true, { transform: booleanAttribute });

  /** Libellé du bouton de fermeture, pour les lecteurs d'écran. */
  readonly libelleFermeture = input($localize`:@@sharedUi.modal.libelleFermeture:Fermer`);

  /** Demande de fermeture émise par l'utilisateur (Échap, arrière-plan, bouton). */
  readonly fermeture = output<void>();

  protected readonly idTitre = `modale-titre-${(compteur += 1)}`;
  protected readonly idDescription = `${this.idTitre}-description`;

  protected readonly classesPanneau = computed(() =>
    [
      'flex max-h-[90vh] w-full flex-col gap-4 overflow-y-auto rounded-t-lg bg-surface p-4',
      'sm:rounded-lg',
      this.taille() === 'large' ? 'sm:max-w-3xl' : 'sm:max-w-lg',
    ].join(' '),
  );

  constructor() {
    // Effet de bord réel : empêcher le défilement de la page derrière le dialogue. Sans cela,
    // la molette fait défiler l'arrière-plan, ce qui désoriente et laisse croire à une fermeture.
    effect((nettoyage) => {
      if (!this.ouverte()) {
        return;
      }

      const corps = this.document.body;
      const valeurInitiale = corps.style.overflow;
      corps.style.overflow = 'hidden';

      nettoyage(() => {
        corps.style.overflow = valeurInitiale;
      });
    });
  }

  /** Ne ferme que si le clic a porté sur l'arrière-plan lui-même, pas sur le panneau. */
  protected surClicArrierePlan(evenement: MouseEvent): void {
    if (this.fermetureParArrierePlan() && evenement.target === evenement.currentTarget) {
      this.fermeture.emit();
    }
  }
}
