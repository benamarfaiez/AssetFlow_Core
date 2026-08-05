import { DOCUMENT } from '@angular/common';
import { Injectable, effect, inject, signal } from '@angular/core';

/**
 * Préférence de thème de l'utilisateur.
 * `auto` s'aligne sur le réglage du système d'exploitation (`prefers-color-scheme`).
 */
export type Theme = 'auto' | 'light' | 'dark';

/** Clé de stockage local. Préfixée pour ne pas heurter une autre application de la même origine. */
const CLE_STOCKAGE = 'assetflow.theme';

/** Attribut porté par l'élément racine ; `styles.css` s'y accroche pour forcer `color-scheme`. */
const ATTRIBUT = 'data-theme';

/**
 * Préférence de thème de l'application.
 *
 * Le choix explicite l'emporte **dans les deux sens** sur le réglage du système : forcer le
 * thème clair sous un système sombre fonctionne autant que l'inverse. En mode `auto`, aucun
 * attribut n'est posé et c'est `color-scheme: light dark` qui laisse le système décider.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);

  private readonly _theme = signal<Theme>('auto');

  /** Préférence courante. */
  readonly theme = this._theme.asReadonly();

  constructor() {
    this._theme.set(this.lirePreference());

    // Effet de bord réel — refléter la préférence sur le document et la retenir. Ce n'est pas
    // une dérivation d'état : `effect()` est ici à sa place.
    effect(() => this.appliquer(this._theme()));
  }

  /** Retient une préférence et l'applique immédiatement. */
  definir(theme: Theme): void {
    this._theme.set(theme);
  }

  private appliquer(theme: Theme): void {
    const racine = this.document.documentElement;

    if (theme === 'auto') {
      racine.removeAttribute(ATTRIBUT);
    } else {
      racine.setAttribute(ATTRIBUT, theme);
    }

    // Le stockage local peut être indisponible (navigation privée, cookies bloqués) : son
    // échec ne doit pas empêcher le changement de thème, qui est déjà appliqué.
    try {
      if (theme === 'auto') {
        this.document.defaultView?.localStorage.removeItem(CLE_STOCKAGE);
      } else {
        this.document.defaultView?.localStorage.setItem(CLE_STOCKAGE, theme);
      }
    } catch {
      // Préférence non persistée : elle vaudra pour la session en cours seulement.
    }
  }

  private lirePreference(): Theme {
    try {
      const valeur = this.document.defaultView?.localStorage.getItem(CLE_STOCKAGE);
      return valeur === 'light' || valeur === 'dark' ? valeur : 'auto';
    } catch {
      return 'auto';
    }
  }
}
