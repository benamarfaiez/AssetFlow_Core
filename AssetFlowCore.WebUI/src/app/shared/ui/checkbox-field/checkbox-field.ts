import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { suivreEtatControle } from '../../forms/etat-controle';
import { premierMessageDeValidation } from '../../i18n/messages-validation';

let compteur = 0;

/**
 * Case à cocher construite sur un `<input type="checkbox">` **natif** : l'état coché, la
 * bascule par la barre d'espace et l'annonce par les lecteurs d'écran sont ceux du navigateur.
 *
 * Approche A (voir `TextField`). Le libellé est cliquable, ce qui élargit la cible bien au-delà
 * du carré de la case.
 */
@Component({
  selector: 'app-checkbox-field',
  imports: [ReactiveFormsModule],
  templateUrl: './checkbox-field.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CheckboxField {
  readonly controle = input.required<FormControl<boolean>>();

  readonly label = input.required<string>();

  readonly aide = input<string | null>(null);

  readonly messages = input<Readonly<Record<string, string>>>({});

  /**
   * `Validators.requiredTrue` publie son erreur sous la clé `required` : le message par défaut
   * dirait « Ce champ est obligatoire », ce qui ne veut rien dire pour une case. La formulation
   * propre à la case s'applique donc d'office, tout en restant remplaçable par l'appelant.
   */
  private readonly messagesEffectifs = computed(() => ({
    required: 'Cette case doit être cochée.',
    ...this.messages(),
  }));

  private readonly evenements = suivreEtatControle(this.controle);

  protected readonly id = `case-${(compteur += 1)}`;
  protected readonly idAide = `${this.id}-aide`;
  protected readonly idErreur = `${this.id}-erreur`;

  protected readonly afficherErreur = computed(() => {
    this.evenements();
    const controle = this.controle();
    return controle.invalid && (controle.touched || controle.dirty);
  });

  protected readonly messageErreur = computed(() =>
    this.afficherErreur()
      ? premierMessageDeValidation(this.controle().errors, this.messagesEffectifs())
      : null,
  );

  protected readonly decritPar = computed(() => {
    const identifiants: string[] = [];
    if (this.aide() !== null) {
      identifiants.push(this.idAide);
    }
    if (this.messageErreur() !== null) {
      identifiants.push(this.idErreur);
    }
    return identifiants.length === 0 ? null : identifiants.join(' ');
  });
}
