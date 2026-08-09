import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/** Reprise du type de `core/theme` sans y créer de dépendance : `shared/` ignore `core/`. */
export type ThemePrefere = 'auto' | 'light' | 'dark';

/** Options proposées, dans l'ordre d'affichage. */
const OPTIONS: readonly { valeur: ThemePrefere; libelle: string }[] = [
  { valeur: 'auto', libelle: $localize`:@@sharedUi.themeToggle.auto:Auto` },
  { valeur: 'light', libelle: $localize`:@@sharedUi.themeToggle.clair:Clair` },
  { valeur: 'dark', libelle: $localize`:@@sharedUi.themeToggle.sombre:Sombre` },
];

/** Compteur d'instances : les boutons radio d'un même groupe partagent un `name` unique. */
let compteur = 0;

/**
 * Bascule de thème — composant de présentation.
 *
 * Trois boutons radio natifs dans un `fieldset` : la navigation par flèches, la sélection et
 * l'annonce du groupe sont assurées par le navigateur, sans code clavier de notre part.
 * Le composant ne connaît ni le service de thème, ni le stockage : il reçoit l'état courant et
 * émet une intention.
 */
@Component({
  selector: 'app-theme-toggle',
  templateUrl: './theme-toggle.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ThemeToggle {
  /** Préférence actuellement retenue. */
  readonly theme = input.required<ThemePrefere>();

  /** Émis lorsque l'utilisateur choisit une autre préférence. */
  readonly themeChange = output<ThemePrefere>();

  protected readonly options = OPTIONS;
  protected readonly nomGroupe = `theme-${(compteur += 1)}`;
}
