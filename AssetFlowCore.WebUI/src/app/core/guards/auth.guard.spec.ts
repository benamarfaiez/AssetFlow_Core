import { TestBed } from '@angular/core/testing';
import {
  provideRouter,
  Router,
  UrlSegment,
  UrlTree,
  type PartialMatchRouteSnapshot,
  type Route,
} from '@angular/router';
import { vi } from 'vitest';
import { AuthTokenService } from '../auth/auth-token.service';
import { EntraAuthService } from '../auth/entra-auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let jetons: AuthTokenService;
  let authentification: EntraAuthService;

  beforeEach(async () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    jetons = TestBed.inject(AuthTokenService);
    authentification = TestBed.inject(EntraAuthService);
    TestBed.inject(Router);

    // Résout `pretAuthentification` (branche « non configuré », instantanée) avant que
    // certains tests ne forcent `estConfigure` à `true` — la garde attend cette promesse et ne
    // doit jamais rester bloquée.
    await authentification.initialiser();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  /** Exécute la garde en contexte d'injection, comme le fait le routeur. */
  function executer(segments: readonly string[] = ['diagnostic']) {
    const segmentsUrl = segments.map((segment) => new UrlSegment(segment, {}));

    return TestBed.runInInjectionContext(() =>
      authGuard({} as Route, segmentsUrl, {} as PartialMatchRouteSnapshot),
    );
  }

  it('laisse passer un utilisateur déjà authentifié', async () => {
    jetons.store('jeton-valide');

    await expect(executer()).resolves.toBe(true);
  });

  it("laisse passer sans authentification tant qu'aucun tenant Entra ID n'est configuré (étape 7.0)", async () => {
    expect(authentification.estConfigure).toBe(false);

    const connexion = vi.spyOn(authentification, 'connecterEtRevenirA');

    await expect(executer()).resolves.toBe(true);
    expect(connexion).not.toHaveBeenCalled();
  });

  it('refuse un utilisateur non authentifié dès qu’un tenant est configuré, avec une UrlTree', async () => {
    // `estConfigure` est calculé à la construction depuis `environment` : forcé ici pour
    // exercer la branche de refus sans dupliquer la simulation d'environnement du service.
    Object.defineProperty(authentification, 'estConfigure', { value: true });
    const connexion = vi
      .spyOn(authentification, 'connecterEtRevenirA')
      .mockResolvedValue(undefined);

    const resultat = await executer(['design-system']);

    expect(resultat).toBeInstanceOf(UrlTree);
    expect((resultat as UrlTree).toString()).toBe('/');
    expect(connexion).toHaveBeenCalledWith('/design-system');
  });

  it('reconstruit un chemin à plusieurs segments, toujours interne par construction', async () => {
    Object.defineProperty(authentification, 'estConfigure', { value: true });
    const connexion = vi
      .spyOn(authentification, 'connecterEtRevenirA')
      .mockResolvedValue(undefined);

    await executer(['diagnostic', 'sous-page']);

    expect(connexion).toHaveBeenCalledWith('/diagnostic/sous-page');
  });
});

describe('authGuard — initialisation de l’authentification encore en cours', () => {
  let authentification: EntraAuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    authentification = TestBed.inject(EntraAuthService);
    TestBed.inject(Router);

    // Contrairement au bloc précédent, `initialiser()` n'est volontairement pas appelée ici :
    // ce bloc vérifie le cas où la garde démarre son exécution avant que l'initialisation de
    // l'authentification ne soit résolue (ex. F5 pendant la connexion silencieuse au démarrage).
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  function executer(segments: readonly string[] = ['diagnostic']) {
    const segmentsUrl = segments.map((segment) => new UrlSegment(segment, {}));

    return TestBed.runInInjectionContext(() =>
      authGuard({} as Route, segmentsUrl, {} as PartialMatchRouteSnapshot),
    );
  }

  it('ne statue pas tant que `pretAuthentification` n’est pas résolue, même démarrée avant elle', async () => {
    let resolue = false;
    const executionGarde = Promise.resolve(executer()).then((resultat) => {
      resolue = true;
      return resultat;
    });

    // Plusieurs tours de micro-tâches sans jamais appeler `initialiser()` : la garde ne doit
    // pas trancher tant que l'initialisation de l'authentification n'est pas terminée.
    await Promise.resolve();
    await Promise.resolve();
    await Promise.resolve();
    expect(resolue).toBe(false);

    await authentification.initialiser();

    await expect(executionGarde).resolves.toBe(true);
    expect(resolue).toBe(true);
  });
});
