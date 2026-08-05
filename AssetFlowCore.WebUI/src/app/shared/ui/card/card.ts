import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Carte : surface délimitée regroupant un contenu et, éventuellement, des actions.
 *
 * Trois emplacements de projection : le contenu par défaut, `[slot=entete]` et `[slot=actions]`.
 * L'en-tête est laissé libre plutôt que réduit à un titre, afin que l'appelant choisisse le
 * niveau de titre correct pour sa page — un niveau imposé ici créerait des sauts de hiérarchie.
 */
@Component({
  selector: 'app-card',
  templateUrl: './card.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
})
export class Card {
  /**
   * Nom accessible de la carte. Renseigné, il transforme la carte en région annoncée
   * (`role="group"`), ce qui n'a de sens que si elle constitue une unité repérable de l'écran.
   */
  readonly ariaLabel = input<string | null>(null);
}
