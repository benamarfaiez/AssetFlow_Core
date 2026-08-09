import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthTokenService } from './auth-token.service';
import { EntraAuthService } from './entra-auth.service';
import { extraireRolesDuJeton, JwtRolesService } from './jwt-roles.service';

/** Encode `valeur` en un segment JWT base64url (RFC 4648 §5, sans remplissage). */
function encoderSegmentBase64Url(valeur: unknown): string {
  return btoa(JSON.stringify(valeur)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

/**
 * Construit un faux JWT à trois segments portant `payload` — signature arbitraire, jamais
 * vérifiée côté client (voir avertissement de `JwtRolesService`).
 */
function creerJeton(payload: unknown): string {
  return `${encoderSegmentBase64Url({ alg: 'none', typ: 'JWT' })}.${encoderSegmentBase64Url(payload)}.signature-non-verifiee`;
}

describe('JwtRolesService', () => {
  let service: JwtRolesService;
  let jetons: AuthTokenService;
  let authentification: EntraAuthService;

  beforeEach(() => {
    // `EntraAuthService` injecte `Router` : nécessaire même si aucun test ci-dessous ne navigue,
    // à l'image d'`auth.guard.spec.ts`.
    TestBed.configureTestingModule({ providers: [provideRouter([])] });

    service = TestBed.inject(JwtRolesService);
    jetons = TestBed.inject(AuthTokenService);
    authentification = TestBed.inject(EntraAuthService);
  });

  describe('quand un tenant Entra ID est configuré', () => {
    beforeEach(() => {
      // `estConfigure` est calculé à la construction depuis `ENTRA_CONFIG` (pas un `computed`) :
      // forcé ici comme dans `auth.guard.spec.ts`, pour exercer la branche « rôles réels » sans
      // dupliquer la simulation d'environnement d'`entra-auth.service.spec.ts`.
      Object.defineProperty(authentification, 'estConfigure', { value: true });
    });

    it('reconnaît le rôle Administrateur porté par le jeton', () => {
      jetons.store(creerJeton({ roles: ['Administrateur'] }));

      expect(service.roles()).toEqual(['Administrateur']);
      expect(service.estAdministrateur()).toBe(true);
    });

    it('refuse un rôle différent d’Administrateur', () => {
      jetons.store(creerJeton({ roles: ['Technicien'] }));

      expect(service.roles()).toEqual(['Technicien']);
      expect(service.estAdministrateur()).toBe(false);
    });

    it('accepte une revendication `roles` en chaîne unique, pas seulement en tableau', () => {
      jetons.store(creerJeton({ roles: 'Administrateur' }));

      expect(service.roles()).toEqual(['Administrateur']);
      expect(service.estAdministrateur()).toBe(true);
    });

    it('renvoie un tableau de rôles vide sans jeton, sans lever d’exception', () => {
      expect(jetons.token()).toBeNull();

      expect(() => service.roles()).not.toThrow();
      expect(service.roles()).toEqual([]);
      expect(service.estAdministrateur()).toBe(false);
    });

    it('renvoie un tableau vide quand le jeton ne porte aucune revendication `roles`', () => {
      jetons.store(creerJeton({ sub: 'utilisatrice-1' }));

      expect(service.roles()).toEqual([]);
      expect(service.estAdministrateur()).toBe(false);
    });

    it('renvoie un tableau vide pour un jeton malformé (payload non-JSON), sans lever d’exception', () => {
      jetons.store('en-tete.pas-du-json-valide.signature');

      expect(() => service.roles()).not.toThrow();
      expect(service.roles()).toEqual([]);
      expect(service.estAdministrateur()).toBe(false);
    });

    it('renvoie un tableau vide pour un jeton n’ayant pas trois segments', () => {
      jetons.store('deux-segments-seulement.ici');

      expect(service.roles()).toEqual([]);
    });
  });

  describe("quand aucun tenant Entra ID n'est configuré (étape 7.0 non réalisée)", () => {
    it('laisse passer `estAdministrateur`, comme `authGuard`, faute de tenant', () => {
      expect(authentification.estConfigure).toBe(false);
      expect(jetons.isAuthenticated()).toBe(false);

      expect(service.estAdministrateur()).toBe(true);
    });

    it('laisse passer `estAdministrateur` même avec un jeton ne portant pas le rôle', () => {
      // Cas d'école, `EntraAuthService` n'émettant aucun jeton sans tenant configuré : vérifie
      // que le laissez-passer est bien inconditionnel, et non une simple conséquence de
      // l'absence de jeton.
      jetons.store(creerJeton({ roles: ['Technicien'] }));

      expect(service.roles()).toEqual(['Technicien']);
      expect(service.estAdministrateur()).toBe(true);
    });
  });
});

describe('extraireRolesDuJeton', () => {
  it('renvoie un tableau vide pour un jeton nul', () => {
    expect(extraireRolesDuJeton(null)).toEqual([]);
  });

  it('renvoie un tableau vide pour un jeton à un seul segment', () => {
    expect(extraireRolesDuJeton('segment-unique')).toEqual([]);
  });

  it('renvoie un tableau vide pour un jeton à quatre segments', () => {
    expect(extraireRolesDuJeton('a.b.c.d')).toEqual([]);
  });

  it('renvoie un tableau vide pour un segment de charge utile hors alphabet base64url', () => {
    expect(extraireRolesDuJeton('en-tete.!!!pas-du-base64!!!.signature')).toEqual([]);
  });

  it('renvoie un tableau vide quand le payload décodé n’est pas un objet', () => {
    const jeton = `en-tete.${encoderSegmentBase64Url(42)}.signature`;

    expect(extraireRolesDuJeton(jeton)).toEqual([]);
  });

  it('ignore les éléments non textuels d’un tableau de rôles hétérogène', () => {
    const jeton = creerJeton({ roles: ['Administrateur', 42, null] });

    expect(extraireRolesDuJeton(jeton)).toEqual(['Administrateur']);
  });

  it('renvoie un tableau vide quand `roles` n’est ni une chaîne ni un tableau', () => {
    const jeton = creerJeton({ roles: 42 });

    expect(extraireRolesDuJeton(jeton)).toEqual([]);
  });
});
