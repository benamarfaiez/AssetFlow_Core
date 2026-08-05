import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
} from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { suivreEtatControle } from '../../forms/etat-controle';
import { premierMessageDeValidation } from '../../i18n/messages-validation';

let compteur = 0;

/**
 * Zone de texte multiligne — description d'anomalie, compte rendu de résolution, motif de
 * transfert. Approche A (voir `TextField`).
 */
@Component({
  selector: 'app-textarea-field',
  imports: [ReactiveFormsModule],
  templateUrl: './textarea-field.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TextareaField {
  readonly controle = input.required<FormControl<string>>();

  readonly label = input.required<string>();

  /** Hauteur initiale, en lignes de texte. La zone reste redimensionnable par l'utilisateur. */
  readonly lignes = input(4);

  readonly aide = input<string | null>(null);

  readonly placeholder = input('');

  readonly longueurMax = input<number | null>(null);

  readonly requis = input(false, { transform: booleanAttribute });

  readonly compteurCaracteres = input(false, { transform: booleanAttribute });

  readonly messages = input<Readonly<Record<string, string>>>({});

  private readonly evenements = suivreEtatControle(this.controle);

  protected readonly id = `zone-${(compteur += 1)}`;
  protected readonly idAide = `${this.id}-aide`;
  protected readonly idErreur = `${this.id}-erreur`;

  protected readonly afficherErreur = computed(() => {
    this.evenements();
    const controle = this.controle();
    return controle.invalid && (controle.touched || controle.dirty);
  });

  protected readonly messageErreur = computed(() =>
    this.afficherErreur()
      ? premierMessageDeValidation(this.controle().errors, this.messages())
      : null,
  );

  protected readonly nombreCaracteres = computed(() => {
    this.evenements();
    return this.controle().value.length;
  });

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

  protected readonly classesChamp = computed(() =>
    [
      'w-full rounded-md border bg-surface px-3 py-2 text-texte',
      'placeholder:text-texte-discret',
      'disabled:bg-surface-creuse disabled:text-texte-discret disabled:cursor-not-allowed',
      this.afficherErreur() ? 'border-danger' : 'border-bordure-controle',
    ].join(' '),
  );
}
