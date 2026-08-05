import { ProblemDetails } from './problem-details.model';

/**
 * Nature de l'échec, dérivée du code de statut. Elle permet à un écran de choisir sa réaction
 * sans réinterpréter les codes HTTP : reporter des messages sur un formulaire, proposer un
 * rechargement, ou afficher un message générique.
 */
export type ApiErrorKind =
  /** 400 accompagné du dictionnaire `errors` : chaque message vise un champ précis. */
  | 'validation'
  /** 400 sans dictionnaire : règle métier refusée ou donnée d'entrée invalide. */
  | 'business'
  /** 404 : la ressource désignée par l'URI n'existe pas. */
  | 'notFound'
  /** 409 : la ressource a changé depuis sa lecture (`RowVersion`), la saisie doit être rejouée. */
  | 'conflict'
  /** 5xx : défaillance du service. Le détail technique n'est jamais présenté à l'utilisateur. */
  | 'server'
  /** Aucune réponse reçue : hors ligne, serveur arrêté, certificat refusé. `status` vaut 0. */
  | 'network';

/** Éléments constitutifs d'une {@link ApiError}. */
export interface ApiErrorDetails {
  readonly kind: ApiErrorKind;
  readonly status: number;
  readonly title: string;
  readonly message: string;
  readonly fieldErrors?: Readonly<Record<string, readonly string[]>>;
  readonly traceId?: string | null;
  readonly problemDetails?: ProblemDetails | null;
}

/**
 * Erreur d'API normalisée. Toute erreur HTTP est traduite une seule fois, dans
 * `errorInterceptor`, ce qui dispense les écrans de manipuler `HttpErrorResponse`.
 *
 * C'est une véritable `Error` : elle conserve une pile d'appels et se teste par
 * `instanceof ApiError`.
 */
export class ApiError extends Error {
  /** Nature de l'échec, à préférer au code de statut brut. */
  readonly kind: ApiErrorKind;

  /** Code de statut HTTP ; 0 lorsque aucune réponse n'a été reçue. */
  readonly status: number;

  /** Titre renvoyé par l'API, à visée technique (journalisation, diagnostic). */
  readonly title: string;

  /**
   * Messages de validation par champ, **clés en `camelCase`** — converties depuis le
   * `PascalCase` du backend pour correspondre aux noms de contrôles d'un formulaire Angular.
   * Vide pour toute erreur qui n'est pas de validation.
   */
  readonly fieldErrors: Readonly<Record<string, readonly string[]>>;

  /** Identifiant de trace joint aux 500, à communiquer au support. */
  readonly traceId: string | null;

  /** Charge utile d'origine, conservée pour le diagnostic. `null` si le corps était illisible. */
  readonly problemDetails: ProblemDetails | null;

  constructor(details: ApiErrorDetails) {
    super(details.message);
    this.name = 'ApiError';
    this.kind = details.kind;
    this.status = details.status;
    this.title = details.title;
    this.fieldErrors = details.fieldErrors ?? {};
    this.traceId = details.traceId ?? null;
    this.problemDetails = details.problemDetails ?? null;
  }

  /** Messages de validation visant le champ donné ; tableau vide s'il n'en porte aucun. */
  messagesFor(field: string): readonly string[] {
    return this.fieldErrors[field] ?? [];
  }

  /** Vrai si au moins un message de validation cible un champ. */
  get hasFieldErrors(): boolean {
    return Object.keys(this.fieldErrors).length > 0;
  }
}
