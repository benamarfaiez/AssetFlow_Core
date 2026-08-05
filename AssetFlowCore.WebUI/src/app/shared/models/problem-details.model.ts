// Contrat synchronisé depuis le backend .NET — ne pas modifier à la main.
// Source : AssetFlowCore.WebApi/Middlewares/ExceptionHandlingMiddleware.cs
// Resynchroniser avec : /sync-api-dtos AssetFlowCore.WebApi/Middlewares/ExceptionHandlingMiddleware.cs

/**
 * Corps d'erreur au format `ProblemDetails` (RFC 7807), produit par
 * `ExceptionHandlingMiddleware` avec le type de contenu `application/problem+json`.
 *
 * `title`, `status` et `detail` sont toujours renseignés ; `type` et `instance` ne sont
 * jamais alimentés par le middleware, d'où leur caractère facultatif.
 */
export interface ProblemDetails {
  readonly title?: string;
  readonly status?: number;
  readonly detail?: string;

  /**
   * Erreurs de validation FluentValidation, présentes uniquement sur les 400 de validation.
   * Les clés sont les **noms de propriétés C# en `PascalCase`** : le sérialiseur n'applique
   * pas de politique de nommage aux clés de dictionnaire, contrairement aux noms de propriétés.
   */
  readonly errors?: Readonly<Record<string, readonly string[]>>;

  /** Identifiant de trace joint aux 500, à communiquer au support. */
  readonly traceId?: string;

  readonly type?: string;
  readonly instance?: string;
}
