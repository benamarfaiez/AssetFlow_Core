import { Injectable, computed, inject } from '@angular/core';
import { AuthTokenService } from './auth-token.service';
import { EntraAuthService } from './entra-auth.service';

/**
 * Nom de la revendication portant les rôles dans le jeton JWT — aligné sur
 * `Authentication:Entra:RoleClaimType` (`AssetFlowCore.WebApi/appsettings.json`), qui vaut
 * `"roles"` côté API.
 */
const REVENDICATION_ROLES = 'roles';

/**
 * Rôle réservé aux actions d'administration (`AssetFlowCore.WebApi/Authorization/Roles.cs`) :
 * remise en service d'un actif au rebut, activation/désactivation d'équipe (Lot 5).
 */
const ROLE_ADMINISTRATEUR = 'Administrateur';

/**
 * Expose, à titre **purement ergonomique**, les rôles portés par le jeton JWT déjà détenu par
 * `AuthTokenService` — pour masquer une action que l'utilisateur courant n'a pas le droit
 * d'exécuter, plutôt que de le laisser essuyer un 403.
 *
 * ⚠️ Ce service ne vérifie **aucune signature** et n'a pas à le faire : il relit, côté client,
 * une revendication déjà portée par un jeton que le navigateur détient de toute façon et qui
 * accompagne chaque requête vers l'API. La seule décision qui compte est prise côté API
 * (`[Authorize(Roles = ...)]` sur `TeamsController` et `AssetsController`), indépendamment de ce
 * que cette classe expose ici — un rôle falsifié dans un jeton n'obtiendrait jamais gain de
 * cause là-bas.
 *
 * Service compagnon distinct d'`AuthTokenService` plutôt qu'extension de celui-ci : c'est le
 * choix retenu ici. `AuthTokenService` reste un simple détenteur de jeton, sans aucune logique
 * au-delà de `store`/`clear`, tandis que le décodage d'un JWT (base64url, JSON, gestion
 * défensive d'un jeton malformé) est une responsabilité distincte, avec ses propres cas limites,
 * qui mérite sa propre unité testable — même principe de séparation qui distingue déjà
 * `EntraAuthService` (orchestration du flux de connexion) d'`AuthTokenService` (stockage).
 */
@Injectable({ providedIn: 'root' })
export class JwtRolesService {
  private readonly jetons = inject(AuthTokenService);
  private readonly authentification = inject(EntraAuthService);

  /**
   * Rôles portés par le jeton courant, ou tableau vide tant qu'aucun jeton n'est détenu ou que
   * son contenu est illisible. Dérivé de `token()` par `computed()` — jamais recopié dans un
   * `signal` séparé, pour ne garder qu'une seule source de vérité.
   *
   * Ne tient volontairement pas compte d'`estConfigure` : ce signal reflète fidèlement le jeton,
   * sans exception. C'est `estAdministrateur`, ci-dessous, qui porte la décision d'ergonomie liée
   * à l'absence de tenant.
   */
  readonly roles = computed(() => extraireRolesDuJeton(this.jetons.token()));

  /**
   * Vrai si l'utilisateur courant porte le rôle `Administrateur`. **Ergonomie uniquement** :
   * masquer une action pour un rôle absent n'est pas une protection — voir l'avertissement de
   * classe ci-dessus, l'API tranche réellement quoi que ce signal affiche.
   *
   * Cas `!estConfigure` (aucun tenant Entra ID, étape 7.0 non réalisée) : laissez-passer
   * (`true`), délibérément aligné sur la même branche d'`authGuard`
   * (`core/guards/auth.guard.ts`) et pour la même raison. Sans tenant, `EntraAuthService`
   * n'émet jamais de jeton : `roles()` resterait vide en permanence et masquerait ces actions
   * pour tout le monde, y compris en développement/démo, sans le moindre bénéfice de sécurité
   * réel — l'API les refuse de toute façon faute même d'un jeton à présenter (401). Dès qu'un
   * tenant est configuré, ce laissez-passer disparaît : place au contenu réel de `roles()`.
   */
  readonly estAdministrateur = computed(() => {
    if (!this.authentification.estConfigure) {
      return true;
    }

    return this.roles().includes(ROLE_ADMINISTRATEUR);
  });
}

/**
 * Décode la revendication `roles` d'un jeton JWT, sans vérifier sa signature (voir avertissement
 * de {@link JwtRolesService}). Ne lève jamais d'exception : un jeton absent ou malformé (mauvais
 * nombre de segments, payload non-JSON, revendication d'une forme inattendue) produit un tableau
 * vide plutôt qu'une erreur qui remonterait jusqu'au rendu d'un écran.
 *
 * Accepte aussi bien un tableau de rôles (forme habituelle des « App Roles » Entra ID dès que
 * plusieurs rôles sont assignés) qu'une chaîne unique, selon ce que l'émetteur a produit.
 */
export function extraireRolesDuJeton(jeton: string | null): readonly string[] {
  if (jeton === null) {
    return [];
  }

  const segments = jeton.split('.');

  if (segments.length !== 3) {
    return [];
  }

  try {
    const charge: unknown = JSON.parse(decoderSegmentBase64Url(segments[1]));

    if (typeof charge !== 'object' || charge === null) {
      return [];
    }

    const revendication = (charge as Record<string, unknown>)[REVENDICATION_ROLES];

    if (typeof revendication === 'string') {
      return [revendication];
    }

    if (Array.isArray(revendication)) {
      return revendication.filter((valeur): valeur is string => typeof valeur === 'string');
    }

    return [];
  } catch {
    // Base64url invalide (`atob` refuse un caractère hors alphabet) ou JSON invalide : jeton
    // traité comme dépourvu de rôle plutôt que remonté en erreur.
    return [];
  }
}

/** Décode un segment JWT base64url (RFC 4648 §5, sans remplissage) en texte UTF-8. */
function decoderSegmentBase64Url(segment: string): string {
  const base64 = segment.replace(/-/g, '+').replace(/_/g, '/');
  const complement = (4 - (base64.length % 4)) % 4;
  const binaire = atob(base64 + '='.repeat(complement));
  const octets = Uint8Array.from(binaire, (caractere) => caractere.charCodeAt(0));

  return new TextDecoder('utf-8').decode(octets);
}
