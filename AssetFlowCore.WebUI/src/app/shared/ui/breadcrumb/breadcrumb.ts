import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Étape du fil d'Ariane. Sans `lien`, l'étape est affichée comme texte. */
export interface EtapeFilAriane {
  readonly libelle: string;
  /** Chemin de navigation. Absent pour la page courante. */
  readonly lien?: string;
}

/**
 * Fil d'Ariane.
 *
 * `<nav>` nommé, liste ordonnée, et `aria-current="page"` sur la dernière étape : la position
 * dans la hiérarchie est annoncée, pas seulement dessinée. Les séparateurs sont décoratifs
 * (`aria-hidden`), sans quoi un lecteur d'écran énoncerait « barre oblique » entre chaque étape.
 *
 * **Écart assumé au périmètre de `shared/`** : ce composant importe `RouterLink`. Un fil d'Ariane
 * accessible exige de vraies ancres — clic du milieu, ouverture dans un onglet, menu contextuel.
 * `RouterLink` est une directive de présentation ; le composant n'injecte jamais `Router` et ne
 * navigue pas de lui-même.
 */
@Component({
  selector: 'app-breadcrumb',
  imports: [RouterLink],
  templateUrl: './breadcrumb.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Breadcrumb {
  readonly etapes = input.required<readonly EtapeFilAriane[]>();

  /** Nom accessible du fil, distinct d'une éventuelle autre navigation de la page. */
  readonly ariaLabel = input("Fil d'Ariane");

  protected readonly derniereEtape = computed(() => this.etapes().length - 1);
}
