import { ChangeDetectionStrategy, Component, booleanAttribute, input, output } from '@angular/core';

/**
 * Message d'erreur d'un écran ou d'un bloc.
 *
 * `role="alert"` fait annoncer le message dès son apparition : une erreur survenue après une
 * action de l'utilisateur ne doit pas rester silencieuse pour un lecteur d'écran.
 *
 * Le composant reçoit un message **déjà rédigé pour un humain** — celui d'`ApiError`, jamais le
 * `detail` technique d'une 500. L'identifiant de trace, lui, est affiché : il n'expose rien et
 * permet au support de retrouver l'incident dans les journaux.
 */
@Component({
  selector: 'app-error-message',
  templateUrl: './error-message.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ErrorMessage {
  /** Message destiné à l'utilisateur. */
  readonly message = input.required<string>();

  /** Intitulé facultatif, quand le contexte de l'échec n'est pas évident. */
  readonly titre = input<string | null>(null);

  /** Identifiant de trace à communiquer au support (erreurs serveur). */
  readonly traceId = input<string | null>(null);

  /** Affiche un bouton « Réessayer ». */
  readonly reessayable = input(false, { transform: booleanAttribute });

  /** Émis lorsque l'utilisateur demande une nouvelle tentative. */
  readonly reessai = output<void>();
}
