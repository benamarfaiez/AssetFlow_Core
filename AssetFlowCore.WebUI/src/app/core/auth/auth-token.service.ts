import { Injectable, computed, signal } from '@angular/core';

/**
 * Détenteur du jeton d'accès.
 *
 * ⚠️ **L'API n'a aujourd'hui aucune authentification** : aucun `AddAuthentication`, aucun
 * `[Authorize]`, aucun endpoint d'émission de jeton (`Program.cs` appelle `UseAuthorization()`
 * sans schéma). Ce service existe donc **sans source** : le jeton reste `null` et
 * `authTokenInterceptor` n'a rien à attacher.
 *
 * Le Lot 7 branchera l'alimentation (`store`) sur le flux d'authentification retenu
 * (décision 0.1). Rien d'autre n'aura à changer : ni l'interceptor, ni les services d'API.
 *
 * Aucune persistance n'est mise en place ici. Le choix du support (mémoire, `sessionStorage`,
 * cookie `HttpOnly`) relève de la décision 0.1 et conditionne l'exposition au vol de jeton.
 */
@Injectable({ providedIn: 'root' })
export class AuthTokenService {
  private readonly _token = signal<string | null>(null);

  /** Jeton courant, ou `null` tant qu'aucune authentification n'est en place. */
  readonly token = this._token.asReadonly();

  /** Vrai lorsqu'un jeton est disponible — donc faux en permanence avant le Lot 7. */
  readonly isAuthenticated = computed(() => this._token() !== null);

  /** Enregistre le jeton émis par le fournisseur d'identité (Lot 7). */
  store(token: string): void {
    this._token.set(token);
  }

  /** Oublie le jeton courant (déconnexion, expiration). */
  clear(): void {
    this._token.set(null);
  }
}
