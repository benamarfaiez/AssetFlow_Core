import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, TemplateRef, computed, input } from '@angular/core';

/**
 * Description d'une colonne.
 *
 * `valeur` rend le contenu textuel — il sert au rendu par défaut **et** à la vue en cartes.
 * `gabarit` permet un rendu riche (badge, lien, actions) : il remplace alors `valeur`, qui reste
 * néanmoins requis pour que la colonne conserve un texte exploitable.
 */
export interface ColonneTable<T> {
  /** Identifiant stable de la colonne, utilisé pour le suivi de liste. */
  readonly cle: string;

  /** Intitulé affiché en en-tête, et libellé de la donnée dans la vue en cartes. */
  readonly entete: string;

  /** Texte de la cellule. */
  readonly valeur: (ligne: T) => string;

  /** Rendu personnalisé. Le contexte expose la ligne en `$implicit`. */
  readonly gabarit?: TemplateRef<{ $implicit: T }>;

  /** Exclut la colonne de la vue en cartes — pour une donnée secondaire sur petit écran. */
  readonly masquerEnCarte?: boolean;
}

/**
 * Table de données **responsive**.
 *
 * Deux rendus coexistent dans le gabarit et c'est le CSS qui choisit : la table à partir de
 * `md`, une liste de cartes en dessous. Le rendu masqué l'est par `display: none`, donc retiré
 * de l'arbre d'accessibilité : rien n'est annoncé deux fois. Ce choix évite d'interroger
 * `matchMedia` en JavaScript, et le basculement suit aussi le **zoom** — à 200 %, la largeur
 * en pixels CSS diminue et la vue en cartes prend le relais, ce qui est exactement l'attendu.
 *
 * Sur les écrans intermédiaires, la table reste dans un conteneur à défilement horizontal
 * **focusable et nommé** (`role="region"`, `tabindex="0"`) : le défilement est alors atteignable
 * au clavier, au lieu d'être une troncature silencieuse.
 */
@Component({
  selector: 'app-data-table',
  imports: [NgTemplateOutlet],
  templateUrl: './data-table.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
})
export class DataTable<T> {
  readonly lignes = input.required<readonly T[]>();

  readonly colonnes = input.required<readonly ColonneTable<T>[]>();

  /**
   * Clé stable d'une ligne, pour le suivi de liste. Un index ne convient pas : il ferait
   * recréer les lignes à chaque tri ou filtrage.
   */
  readonly cleLigne = input.required<(ligne: T) => string>();

  /**
   * Ce que contient la table, en une phrase. Sert de `<caption>` (masqué visuellement) et de nom
   * accessible à la zone de défilement : « Inventaire des actifs », « Incidents en cours ».
   */
  readonly legende = input.required<string>();

  readonly messageVide = input(
    $localize`:@@sharedUi.dataTable.messageVide:Aucune donnée à afficher.`,
  );

  protected readonly colonnesEnCarte = computed(() =>
    this.colonnes().filter((colonne) => colonne.masquerEnCarte !== true),
  );
}
