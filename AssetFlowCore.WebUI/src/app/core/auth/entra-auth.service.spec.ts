import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import type {
  AccountInfo,
  AuthenticationResult,
  IPublicClientApplication,
} from '@azure/msal-browser';
import { InteractionRequiredAuthError } from '@azure/msal-browser';
import { vi } from 'vitest';
import type { EntraIdConfig } from '../../../environments/environment.model';
import { AuthTokenService } from './auth-token.service';
import { estCheminInterne, EntraAuthService, MSAL_CLIENT_FACTORY } from './entra-auth.service';
import { ENTRA_CONFIG } from './msal.config';

/**
 * `EntraAuthService.estConfigure` lit {@link ENTRA_CONFIG} à la construction : le système de
 * test unitaire du dépôt (`@angular/build:unit-test`) n'autorise pas `vi.mock` sur les imports
 * relatifs — substituer ce jeton via `TestBed` est le seul moyen de simuler un tenant Entra ID
 * configuré. Le reste du dépôt (dev comme production) commite volontairement des valeurs vides
 * (étape 7.0) : voir `entra-auth.service.inerte.spec.ts` pour ce comportement réel, non simulé.
 */
const CONFIG_DE_TEST: EntraIdConfig = {
  authority: 'https://login.microsoftonline.com/tenant-de-test',
  clientId: 'client-de-test',
  redirectUri: 'https://app.test/',
  apiScope: 'api://client-de-test/access_as_user',
};

const COMPTE: AccountInfo = {
  homeAccountId: 'compte-1',
  environment: 'login.microsoftonline.com',
  tenantId: 'tenant-de-test',
  username: 'utilisatrice@test.local',
  localAccountId: 'compte-1',
};

/** Double minimal d'`IPublicClientApplication` : seules les méthodes utilisées par le service sont réelles. */
class ClientMsalSimule {
  initialize = vi.fn(async (): Promise<void> => undefined);
  handleRedirectPromise = vi.fn(async (): Promise<AuthenticationResult | null> => null);
  acquireTokenSilent = vi.fn(async (): Promise<AuthenticationResult> => resultatAuthentification());
  loginRedirect = vi.fn(async (): Promise<void> => undefined);
  getAllAccounts = vi.fn((): AccountInfo[] => []);
  getActiveAccount = vi.fn((): AccountInfo | null => null);
  setActiveAccount = vi.fn();
}

function resultatAuthentification(
  surcharge: Partial<AuthenticationResult> = {},
): AuthenticationResult {
  return {
    authority: 'https://login.microsoftonline.com/tenant-de-test',
    uniqueId: 'unique-1',
    tenantId: 'tenant-de-test',
    scopes: ['api://client-de-test/access_as_user'],
    account: COMPTE,
    idToken: 'id-token-simule',
    idTokenClaims: {},
    accessToken: 'jeton-simule',
    fromCache: false,
    expiresOn: new Date(Date.now() + 60 * 60 * 1000),
    tokenType: 'Bearer',
    correlationId: 'correlation-1',
    ...surcharge,
  };
}

describe('EntraAuthService', () => {
  let client: ClientMsalSimule;
  let service: EntraAuthService;
  let jetons: AuthTokenService;
  let router: Router;

  beforeEach(() => {
    client = new ClientMsalSimule();

    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: ENTRA_CONFIG, useValue: CONFIG_DE_TEST },
        {
          provide: MSAL_CLIENT_FACTORY,
          useValue: () => client as unknown as IPublicClientApplication,
        },
      ],
    });

    service = TestBed.inject(EntraAuthService);
    jetons = TestBed.inject(AuthTokenService);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('se déclare configuré dès qu’un identifiant client est présent', () => {
    expect(service.estConfigure).toBe(true);
  });

  it('traite un retour de redirection : stocke le jeton et restaure la destination interne', async () => {
    client.handleRedirectPromise.mockResolvedValueOnce(
      resultatAuthentification({ state: '/design-system' }),
    );
    const navigation = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    await service.initialiser();
    await service.pretAuthentification;

    expect(jetons.token()).toBe('jeton-simule');
    expect(navigation).toHaveBeenCalledWith('/design-system');
  });

  it('ignore une destination externe portée par l’état de la requête OIDC', async () => {
    client.handleRedirectPromise.mockResolvedValueOnce(
      resultatAuthentification({ state: '//hote-externe.test' }),
    );
    const navigation = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    await service.initialiser();

    expect(navigation).not.toHaveBeenCalled();
  });

  it('sans retour de redirection, tente une connexion silencieuse pour un compte en cache', async () => {
    client.getAllAccounts.mockReturnValue([COMPTE]);

    await service.initialiser();

    expect(client.setActiveAccount).toHaveBeenCalledWith(COMPTE);
    expect(client.acquireTokenSilent).toHaveBeenCalledWith(
      expect.objectContaining({ account: COMPTE }),
    );
    expect(jetons.isAuthenticated()).toBe(true);
  });

  it('reste anonyme sans compte en cache et sans retour de redirection', async () => {
    await service.initialiser();

    expect(client.acquireTokenSilent).not.toHaveBeenCalled();
    expect(jetons.isAuthenticated()).toBe(false);
  });

  it('oublie le jeton quand la connexion silencieuse échoue', async () => {
    client.getAllAccounts.mockReturnValue([COMPTE]);
    client.acquireTokenSilent.mockRejectedValueOnce(
      new InteractionRequiredAuthError('interaction_required', 'Interaction requise'),
    );
    jetons.store('jeton-perime');

    await service.initialiser();

    expect(jetons.isAuthenticated()).toBe(false);
  });

  it("résout `pretAuthentification` même si l'initialisation échoue", async () => {
    client.initialize.mockRejectedValueOnce(new Error('Autorité injoignable'));

    await service.initialiser();

    await expect(service.pretAuthentification).resolves.toBeUndefined();
    expect(jetons.isAuthenticated()).toBe(false);
  });

  it('reste inerte quand la construction même du client MSAL échoue (`SubtleCrypto` absent, stockage bloqué…)', async () => {
    // Module de test dédié, avec une fabrique qui échoue de façon synchrone dès l'appel — donc
    // avant tout appel à `initialize()` — pour simuler un échec de `new PublicClientApplication(...)`.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: ENTRA_CONFIG, useValue: CONFIG_DE_TEST },
        {
          provide: MSAL_CLIENT_FACTORY,
          useValue: (): never => {
            throw new Error('Construction du client MSAL impossible');
          },
        },
      ],
    });

    const serviceCasse = TestBed.inject(EntraAuthService);
    const jetonsCasse = TestBed.inject(AuthTokenService);

    await serviceCasse.initialiser();

    // `pretAuthentification` ne doit jamais rejeter, même si la construction échoue de façon
    // synchrone : sans cela, `provideAppInitializer` empêcherait tout rendu (écran blanc).
    await expect(serviceCasse.pretAuthentification).resolves.toBeUndefined();
    expect(jetonsCasse.isAuthenticated()).toBe(false);

    // Le client n'a jamais été affecté : `connecterEtRevenirA` doit rester inoffensif plutôt que
    // d'appeler une méthode sur un client cassé.
    await expect(serviceCasse.connecterEtRevenirA('/diagnostic')).resolves.toBeUndefined();
  });

  describe('connecterEtRevenirA', () => {
    beforeEach(async () => {
      // `this.client` n'est construit que par `initialiser()` : sans cet appel, le service se
      // croit non initialisé et `connecterEtRevenirA` se contente d'un avertissement inoffensif.
      await service.initialiser();
    });

    it('lance une redirection avec la portée API et la destination en état de requête', async () => {
      await service.connecterEtRevenirA('/diagnostic');

      expect(client.loginRedirect).toHaveBeenCalledWith(
        expect.objectContaining({
          scopes: ['api://client-de-test/access_as_user'],
          state: '/diagnostic',
        }),
      );
    });

    it('rejette une destination externe : aucun état transmis à MSAL', async () => {
      await service.connecterEtRevenirA('https://hote-externe.test/vol-de-jeton');

      expect(client.loginRedirect).toHaveBeenCalledWith(
        expect.objectContaining({ state: undefined }),
      );
    });

    it('ne relance pas une connexion déjà en cours', async () => {
      let resoudre: (() => void) | undefined;
      client.loginRedirect.mockImplementationOnce(
        () => new Promise<void>((resolve) => (resoudre = resolve)),
      );

      const premierAppel = service.connecterEtRevenirA('/diagnostic');
      const secondAppel = service.connecterEtRevenirA('/design-system');

      resoudre?.();
      await Promise.all([premierAppel, secondAppel]);

      expect(client.loginRedirect).toHaveBeenCalledTimes(1);
    });
  });

  describe('renouvelerJeton', () => {
    beforeEach(async () => {
      await service.initialiser();
    });

    it('renvoie `null` sans compte actif', async () => {
      const jeton = await service.renouvelerJeton();

      expect(jeton).toBeNull();
      expect(client.acquireTokenSilent).not.toHaveBeenCalled();
    });

    it('renouvelle et stocke le jeton pour le compte actif', async () => {
      client.getActiveAccount.mockReturnValue(COMPTE);
      client.acquireTokenSilent.mockResolvedValueOnce(
        resultatAuthentification({ accessToken: 'jeton-frais' }),
      );

      const jeton = await service.renouvelerJeton();

      expect(jeton).toBe('jeton-frais');
      expect(jetons.token()).toBe('jeton-frais');
    });

    it('oublie le jeton et renvoie `null` quand le renouvellement échoue', async () => {
      client.getActiveAccount.mockReturnValue(COMPTE);
      client.acquireTokenSilent.mockRejectedValueOnce(
        new InteractionRequiredAuthError('interaction_required', 'Interaction requise'),
      );
      jetons.store('jeton-perime');

      const jeton = await service.renouvelerJeton();

      expect(jeton).toBeNull();
      expect(jetons.isAuthenticated()).toBe(false);
    });

    it('mutualise les appels concurrents : un seul `acquireTokenSilent` pour N appels simultanés', async () => {
      client.getActiveAccount.mockReturnValue(COMPTE);
      let resoudre: ((resultat: AuthenticationResult) => void) | undefined;
      client.acquireTokenSilent.mockImplementationOnce(
        () =>
          new Promise<AuthenticationResult>((resolve) => {
            resoudre = resolve;
          }),
      );

      const appels = [
        service.renouvelerJeton(),
        service.renouvelerJeton(),
        service.renouvelerJeton(),
      ];

      resoudre?.(resultatAuthentification({ accessToken: 'jeton-partage' }));
      const jetonsObtenus = await Promise.all(appels);

      expect(client.acquireTokenSilent).toHaveBeenCalledTimes(1);
      expect(jetonsObtenus).toEqual(['jeton-partage', 'jeton-partage', 'jeton-partage']);
    });

    it('autorise un nouvel appel une fois le renouvellement précédent terminé', async () => {
      client.getActiveAccount.mockReturnValue(COMPTE);
      client.acquireTokenSilent.mockResolvedValue(
        resultatAuthentification({ accessToken: 'jeton-suivant' }),
      );

      await service.renouvelerJeton();
      await service.renouvelerJeton();

      expect(client.acquireTokenSilent).toHaveBeenCalledTimes(2);
    });
  });

  describe('obtenirJetonFrais', () => {
    beforeEach(async () => {
      await service.initialiser();
    });

    it('renvoie une chaîne vide plutôt que `null`, pour `accessTokenFactory` de SignalR', async () => {
      const jeton = await service.obtenirJetonFrais();

      expect(jeton).toBe('');
    });

    it('renvoie le jeton renouvelé quand un compte est actif', async () => {
      client.getActiveAccount.mockReturnValue(COMPTE);
      client.acquireTokenSilent.mockResolvedValueOnce(
        resultatAuthentification({ accessToken: 'jeton-hub' }),
      );

      const jeton = await service.obtenirJetonFrais();

      expect(jeton).toBe('jeton-hub');
    });
  });
});

describe('estCheminInterne', () => {
  it('accepte un chemin absolu interne', () => {
    expect(estCheminInterne('/diagnostic')).toBe(true);
  });

  it('rejette une URL absolue', () => {
    expect(estCheminInterne('https://hote-externe.test/')).toBe(false);
  });

  it('rejette un chemin protocole-relatif', () => {
    expect(estCheminInterne('//hote-externe.test/')).toBe(false);
  });

  it('rejette un chemin relatif sans barre oblique initiale', () => {
    expect(estCheminInterne('diagnostic')).toBe(false);
  });

  it('rejette un chemin contenant un antislash — certains navigateurs le normalisent en `/`', () => {
    expect(estCheminInterne('/\\hote-externe.test')).toBe(false);
  });
});
