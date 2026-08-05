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

/** Option proposée par le sélecteur. La valeur transite telle quelle dans le contrôle. */
export interface OptionSelecteur<T extends string = string> {
  readonly valeur: T;
  readonly libelle: string;
  readonly desactivee?: boolean;
}

let compteur = 0;

/**
 * Sélecteur à liste fermée, construit sur un `<select>` **natif** : ouverture, navigation au
 * clavier, recherche par frappe et rendu adapté au mobile sont ceux du système.
 *
 * Approche A (voir `TextField`). Les libellés affichés sont fournis par l'appelant — c'est là que
 * la traduction des valeurs d'API doit être appliquée, jamais dans ce composant.
 */
@Component({
  selector: 'app-select-field',
  imports: [ReactiveFormsModule],
  templateUrl: './select-field.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SelectField<T extends string = string> {
  readonly controle = input.required<FormControl<T>>();

  readonly label = input.required<string>();

  readonly options = input.required<readonly OptionSelecteur<T>[]>();

  /**
   * Libellé de l'entrée vide, proposée en tête de liste. À `null`, aucune entrée vide :
   * le sélecteur impose alors un choix parmi les options.
   */
  readonly libelleVide = input<string | null>('Sélectionnez…');

  readonly aide = input<string | null>(null);

  readonly requis = input(false, { transform: booleanAttribute });

  readonly messages = input<Readonly<Record<string, string>>>({});

  private readonly evenements = suivreEtatControle(this.controle);

  protected readonly id = `selecteur-${(compteur += 1)}`;
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
      'min-h-(--cible-tactile)',
      'disabled:bg-surface-creuse disabled:text-texte-discret disabled:cursor-not-allowed',
      this.afficherErreur() ? 'border-danger' : 'border-bordure-controle',
    ].join(' '),
  );
}
