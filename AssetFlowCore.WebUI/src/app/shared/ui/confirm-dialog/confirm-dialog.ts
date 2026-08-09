import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';
import { Button, VarianteBouton } from '../button/button';
import { Modal } from '../modal/modal';

/**
 * Tonalité d'une confirmation. Distincte de `Tonalite` (badges et messages d'état, cinq
 * valeurs) : une confirmation n'a besoin que de deux registres — une action destructrice à
 * l'apparence irréversible, ou un simple avertissement.
 */
export type TonaliteConfirmation = 'danger' | 'avertissement';

/**
 * Confirmation prête à l'emploi, composée au-dessus de `Modal` (utilisée ici en composition,
 * jamais modifiée).
 *
 * `ConfirmDialog` ajoute par-dessus `Modal` :
 * - deux actions standard (annulation / confirmation), la seconde stylée selon `tonalite` ;
 * - un état `enCours` qui verrouille le dialogue le temps d'un appel réseau : les deux actions
 *   sont désactivées et toute demande de fermeture de `Modal` (Échap, arrière-plan, bouton ×)
 *   est ignorée plutôt que traitée comme une annulation ;
 * - un slot de contenu pour un champ projeté par l'appelant (motif, sélecteur d'équipe…),
 *   affiché entre le message et les actions.
 *
 * Comme `Modal`, le composant ne se ferme jamais lui-même : il émet une intention
 * (`confirmation` ou `annulation`) et l'appelant décide de remettre `ouverte` à `false` — ce qui
 * permet par exemple de ne fermer qu'après le succès de l'appel déclenché par `confirmation`.
 *
 * Accessibilité : `message` est transmis à `Modal` comme `description`, donc inclus dans le
 * `aria-describedby` du dialogue et lu dès l'ouverture par un lecteur d'écran — aucune région
 * `aria-live` séparée n'est ajoutée ici, ce qui doublerait l'annonce. `Modal` fournit déjà
 * `role="dialog"` et le piège de focus : ce composant n'en ajoute pas de second.
 *
 * `tonalite` ne restylise pas `message` : `Modal` ne permet pas d'injecter une classe dans son
 * paragraphe de description, et afficher une seconde copie du message ailleurs pour la colorer
 * casserait la source unique de l'annonce (double lecture si elle rejoint aussi
 * `aria-describedby`, ou copie invisible d'un lecteur d'écran sinon). `tonalite` pilote donc
 * uniquement la variante du bouton de confirmation ci-dessous ; un avertissement conditionnel
 * propre à un écran (ex. désactivation d'équipe) se projette dans le slot de contenu.
 */
@Component({
  selector: 'app-confirm-dialog',
  imports: [Modal, Button],
  templateUrl: './confirm-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialog {
  readonly ouverte = input.required<boolean>();

  readonly titre = input.required<string>();

  /** Conséquence de l'action, formulée pour un humain. Relayé à `Modal` via `description`. */
  readonly message = input.required<string>();

  readonly libelleConfirmation = input($localize`:@@sharedUi.confirmDialog.confirmer:Confirmer`);

  readonly libelleAnnulation = input($localize`:@@sharedUi.confirmDialog.annuler:Annuler`);

  /**
   * `danger` pour une action destructrice à l'apparence irréversible → bouton `danger`.
   * `avertissement` pour une confirmation moins alarmante → bouton `primaire` : parmi les
   * variantes existantes de `Button` (`primaire` | `secondaire` | `discret` | `danger`), c'est
   * la seule qui reste visuellement affirmative sans emprunter la tonalité d'alerte réservée à
   * `danger` — `secondaire` ou `discret` sous-entendraient à tort que confirmer est l'option
   * secondaire de l'écran.
   */
  readonly tonalite = input<TonaliteConfirmation>('danger');

  /**
   * Traitement en cours : désactive les deux actions, affiche l'état `enCours` sur le bouton de
   * confirmation (spinner + `aria-busy`, déjà fournis par `Button`), et ignore toute demande de
   * fermeture de `Modal` pour ne pas interrompre un appel réseau en vol.
   */
  readonly enCours = input(false, { transform: booleanAttribute });

  /** L'appelant déclenche l'action, puis remet `ouverte` à `false` une fois celle-ci terminée. */
  readonly confirmation = output<void>();

  /** L'appelant remet `ouverte` à `false`. */
  readonly annulation = output<void>();

  protected readonly varianteConfirmation = computed<VarianteBouton>(() =>
    this.tonalite() === 'danger' ? 'danger' : 'primaire',
  );

  /** Le clic sur l'arrière-plan reste une fermeture valide tant qu'aucun traitement n'est en cours. */
  protected readonly fermetureParArrierePlan = computed(() => !this.enCours());

  /**
   * Relais unique des demandes de fermeture de `Modal` (Échap, arrière-plan, bouton ×) et du
   * bouton d'annulation : ignoré pendant `enCours`, sinon traité comme une annulation.
   */
  protected annuler(): void {
    if (this.enCours()) {
      return;
    }
    this.annulation.emit();
  }

  protected confirmer(): void {
    if (this.enCours()) {
      return;
    }
    this.confirmation.emit();
  }
}
