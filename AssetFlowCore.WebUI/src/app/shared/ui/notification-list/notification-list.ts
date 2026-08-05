import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Badge } from '../badge/badge';
import { Tonalite } from '../tonalite';

/**
 * Notification transitoire.
 *
 * Le type porte volontairement le suffixe `Ui` : `Notification` est un type global du navigateur,
 * et l'ombrer exposerait à confondre les deux au premier import oublié.
 */
export interface NotificationUi {
  /** Identifiant stable, utilisé pour le suivi de liste et le rejet. */
  readonly id: string;
  readonly tonalite: Tonalite;
  readonly titre: string;
  readonly message?: string | null;
}

/**
 * Pile de notifications.
 *
 * La région `aria-live="polite"` est **toujours présente dans le document**, même vide : un
 * lecteur d'écran n'annonce les ajouts que dans une région qui existait déjà avant la
 * modification. Créer la région en même temps que le message ne produirait rien.
 *
 * `polite` et non `assertive` : ces messages accompagnent une action, ils ne justifient pas
 * d'interrompre la lecture en cours.
 *
 * La file (empilement, expiration automatique) appartient à un service applicatif de `core/` :
 * ce composant se contente d'afficher ce qu'on lui donne et de signaler les rejets.
 */
@Component({
  selector: 'app-notification-list',
  imports: [Badge],
  templateUrl: './notification-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationList {
  readonly notifications = input.required<readonly NotificationUi[]>();

  /** Émet l'identifiant de la notification que l'utilisateur ferme. */
  readonly rejet = output<string>();
}
