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

/** Compteur d'instances : chaque champ a besoin d'un identifiant unique pour lier son libellé. */
let compteur = 0;

/**
 * Champ de saisie sur une ligne.
 *
 * **Approche A** : le composant reçoit le `FormControl` en entrée (plutôt que d'implémenter
 * `ControlValueAccessor`). L'API reste entièrement typée, sans contrat implicite à respecter, et
 * l'état désactivé fonctionne d'emblée — `control.disable()` agit sur le contrôle lui-même.
 *
 * Le composant **n'ajoute ni ne retire jamais de validateur** : la validation appartient à la
 * feature. Il ne fait qu'afficher l'état du contrôle.
 */
@Component({
  selector: 'app-text-field',
  imports: [ReactiveFormsModule],
  templateUrl: './text-field.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TextField {
  /** Contrôle piloté par la feature. */
  readonly controle = input.required<FormControl<string>>();

  /** Libellé visible, associé au champ par `for`/`id`. */
  readonly label = input.required<string>();

  readonly type = input<'text' | 'email' | 'password' | 'search' | 'tel' | 'url'>('text');

  /** Consigne de saisie affichée sous le champ et référencée par `aria-describedby`. */
  readonly aide = input<string | null>(null);

  readonly placeholder = input('');

  /** Valeur de l'attribut `autocomplete` ; à renseigner pour tout champ d'identité. */
  readonly autocomplete = input<string | null>(null);

  /** Longueur maximale. Alimente aussi le compteur de caractères. */
  readonly longueurMax = input<number | null>(null);

  readonly requis = input(false, { transform: booleanAttribute });

  /** Affiche « n / max » sous le champ. Sans effet si `longueurMax` est absent. */
  readonly compteurCaracteres = input(false, { transform: booleanAttribute });

  /**
   * Messages propres au contexte, par clé d'erreur, prioritaires sur les messages par défaut.
   * La clé `serveur` sert à reporter une erreur renvoyée par l'API sur le champ concerné.
   */
  readonly messages = input<Readonly<Record<string, string>>>({});

  // Rend le rendu réactif à l'état du contrôle : sans cela, un `markAsTouched()` déclenché par
  // la soumission n'afficherait aucun message (voir suivreEtatControle).
  private readonly evenements = suivreEtatControle(this.controle);

  protected readonly id = `champ-${(compteur += 1)}`;
  protected readonly idAide = `${this.id}-aide`;
  protected readonly idErreur = `${this.id}-erreur`;

  /** L'erreur n'apparaît qu'après interaction : jamais à l'ouverture d'un formulaire vierge. */
  protected readonly afficherErreur = computed(() => {
    this.evenements();
    const controle = this.controle();
    return controle.invalid && (controle.touched || controle.dirty);
  });

  protected readonly messageErreur = computed(() => {
    if (!this.afficherErreur()) {
      return null;
    }
    return premierMessageDeValidation(this.controle().errors, this.messages());
  });

  protected readonly nombreCaracteres = computed(() => {
    this.evenements();
    return this.controle().value.length;
  });

  /** Lie le champ à sa consigne et à son message d'erreur, dans cet ordre de lecture. */
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
      'min-h-(--cible-tactile) placeholder:text-texte-discret',
      'disabled:bg-surface-creuse disabled:text-texte-discret disabled:cursor-not-allowed',
      this.afficherErreur() ? 'border-danger' : 'border-bordure-controle',
    ].join(' '),
  );
}
