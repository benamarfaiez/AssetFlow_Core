import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiError, ApiErrorKind } from '../../shared/models/api-error.model';
import { ProblemDetails } from '../../shared/models/problem-details.model';

/**
 * Messages présentés à l'utilisateur lorsque la réponse n'en fournit pas d'exploitable.
 * Ceux du backend sont déjà en français et restent prioritaires quand ils existent.
 */
const MESSAGES = {
  validation: 'Certaines informations saisies sont invalides.',
  business: "L'opération a été refusée.",
  unauthorized: 'Votre session a expiré ou est invalide. Reconnectez-vous pour continuer.',
  forbidden: "Vous n'avez pas les droits nécessaires pour effectuer cette action.",
  notFound: "La ressource demandée n'existe pas ou plus.",
  conflict:
    'Ces données ont été modifiées entre-temps. Rechargez-les avant de valider vos modifications.',
  server:
    "Le service a rencontré une erreur inattendue. Réessayez ; si le problème persiste, communiquez l'identifiant de trace au support.",
  network: 'Le serveur est injoignable. Vérifiez votre connexion réseau, puis réessayez.',
} as const satisfies Record<ApiErrorKind, string>;

/**
 * Traduit toute erreur HTTP en {@link ApiError}. C'est le **seul** endroit où le format
 * `ProblemDetails` de l'API est interprété : les services et les écrans reçoivent une erreur
 * déjà normalisée.
 *
 * Correspondances produites par `ExceptionHandlingMiddleware` :
 * - 400 avec dictionnaire `errors` → `validation` (messages reportables sur les champs) ;
 * - 400 sans dictionnaire → `business` (règle métier refusée, message affichable) ;
 * - 401 → `unauthorized` (jeton absent, expiré ou invalide — `sessionRenewalInterceptor` en a
 *   déjà tenté un renouvellement et un rejeu, en amont dans la chaîne, avant que cette nature
 *   n'atteigne l'appelant) ;
 * - 403 → `forbidden` (autorisation refusée, jamais rejouée) ;
 * - 404 → `notFound` ; 409 → `conflict` (concurrence sur `RowVersion`) ;
 * - 5xx → `server`, avec un message **générique** : le détail technique n'est jamais présenté ;
 * - absence de réponse (`status` 0) → `network`.
 */
export const errorInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((response: unknown) => {
      // Une erreur déjà normalisée (interceptor placé en aval) traverse sans retraitement.
      if (response instanceof ApiError) {
        return throwError(() => response);
      }

      const erreur =
        response instanceof HttpErrorResponse
          ? toApiError(response)
          : new ApiError({
              kind: 'server',
              status: 0,
              title: 'Erreur inattendue',
              message: MESSAGES.server,
            });

      if (!environment.production && (erreur.kind === 'server' || erreur.kind === 'network')) {
        // Journalisation de développement : ces deux natures d'erreur n'exposent aucun détail
        // à l'utilisateur, la console reste le seul moyen de les diagnostiquer localement.
        console.error(
          `[API] ${request.method} ${request.urlWithParams} → ${erreur.status}`,
          erreur.problemDetails ?? erreur.message,
        );
      }

      return throwError(() => erreur);
    }),
  );

/** Convertit une réponse d'erreur HTTP en erreur applicative. */
function toApiError(response: HttpErrorResponse): ApiError {
  const problemDetails = extractProblemDetails(response);
  const detail = problemDetails?.detail?.trim() ?? '';
  const title = problemDetails?.title?.trim() ?? response.statusText ?? 'Erreur';
  const traceId = problemDetails?.traceId ?? null;

  // `status` 0 signale qu'aucune réponse n'est parvenue : réseau coupé, service arrêté, ou
  // certificat refusé. Distinguer ce cas d'un 500 évite d'accuser le serveur à tort.
  if (response.status === 0) {
    return new ApiError({
      kind: 'network',
      status: 0,
      title: 'Serveur injoignable',
      message: MESSAGES.network,
    });
  }

  if (response.status === 400) {
    const fieldErrors = normalizeFieldErrors(problemDetails?.errors);
    const kind: ApiErrorKind = Object.keys(fieldErrors).length > 0 ? 'validation' : 'business';

    return new ApiError({
      kind,
      status: 400,
      title,
      message: detail || (kind === 'validation' ? MESSAGES.validation : MESSAGES.business),
      fieldErrors,
      traceId,
      problemDetails,
    });
  }

  if (response.status === 401) {
    return new ApiError({
      kind: 'unauthorized',
      status: 401,
      title,
      message: detail || MESSAGES.unauthorized,
      traceId,
      problemDetails,
    });
  }

  if (response.status === 403) {
    return new ApiError({
      kind: 'forbidden',
      status: 403,
      title,
      message: detail || MESSAGES.forbidden,
      traceId,
      problemDetails,
    });
  }

  if (response.status === 404) {
    return new ApiError({
      kind: 'notFound',
      status: 404,
      title,
      message: detail || MESSAGES.notFound,
      traceId,
      problemDetails,
    });
  }

  if (response.status === 409) {
    return new ApiError({
      kind: 'conflict',
      status: 409,
      title,
      message: detail || MESSAGES.conflict,
      traceId,
      problemDetails,
    });
  }

  // Tout le reste — 5xx — est traité comme une défaillance de service. Le `detail` est
  // délibérément ignoré : il peut provenir d'un intermédiaire (reverse proxy) et porter des
  // informations d'implémentation.
  return new ApiError({
    kind: 'server',
    status: response.status,
    title,
    message: MESSAGES.server,
    traceId,
    problemDetails,
  });
}

/**
 * Extrait le corps `ProblemDetails`. `HttpClient` livre un objet déjà désérialisé, mais un
 * corps textuel (page d'erreur d'un intermédiaire, JSON tronqué) reste possible : il ne doit
 * pas faire échouer la traduction.
 *
 * Sur échec réseau, `error` porte l'exception du transport (`Error` avec le client `fetch`,
 * `ProgressEvent` avec `XMLHttpRequest`) et non un corps de réponse : ces deux formes sont
 * écartées plutôt que présentées comme un `ProblemDetails`.
 */
function extractProblemDetails(response: HttpErrorResponse): ProblemDetails | null {
  const corps: unknown = response.error;

  if (corps instanceof Error || corps instanceof ProgressEvent) {
    return null;
  }

  if (corps && typeof corps === 'object') {
    return corps as ProblemDetails;
  }

  if (typeof corps === 'string' && corps.trim().startsWith('{')) {
    try {
      return JSON.parse(corps) as ProblemDetails;
    } catch {
      return null;
    }
  }

  return null;
}

/**
 * Convertit les clés du dictionnaire `errors` du `PascalCase` du backend vers le `camelCase`
 * des noms de contrôles d'un formulaire Angular. Les clés composées (`Command.Name`) sont
 * réduites à leur dernier segment, seul à correspondre à un champ de saisie.
 */
function normalizeFieldErrors(
  errors: Readonly<Record<string, readonly string[]>> | undefined,
): Record<string, readonly string[]> {
  if (!errors) {
    return {};
  }

  const normalise: Record<string, readonly string[]> = {};

  for (const [cle, messages] of Object.entries(errors)) {
    const segments = cle.split('.');
    const dernier = segments[segments.length - 1] ?? cle;
    const champ = dernier.charAt(0).toLowerCase() + dernier.slice(1);

    // Deux clés backend peuvent se réduire au même champ : les messages se cumulent.
    normalise[champ] = [...(normalise[champ] ?? []), ...messages];
  }

  return normalise;
}
