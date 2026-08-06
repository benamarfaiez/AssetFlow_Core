import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthTokenService } from './auth-token.service';
import { EntraAuthService } from './entra-auth.service';

/**
 * Fichier séparé de `entra-auth.service.spec.ts` : celui-ci simule un tenant Entra ID configuré
 * via `vi.mock` sur `environments/environment`, ce qui s'applique à tout le fichier. Cette suite
 * vérifie au contraire le comportement réel du dépôt, avec les valeurs placeholder vides commitées
 * tant que l'étape 7.0 (tenant Entra ID) n'est pas réalisée — aucun mock ici.
 */
describe('EntraAuthService — tenant non configuré (étape 7.0 non réalisée)', () => {
  it('part des valeurs placeholder vides du dépôt', () => {
    expect(environment.entra.clientId).toBe('');
  });

  it('se déclare non configuré et reste inerte à l’initialisation', async () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const service = TestBed.inject(EntraAuthService);
    const jetons = TestBed.inject(AuthTokenService);

    expect(service.estConfigure).toBe(false);

    await service.initialiser();
    await expect(service.pretAuthentification).resolves.toBeUndefined();

    expect(jetons.isAuthenticated()).toBe(false);
  });

  it('ignore une demande de connexion plutôt que de tenter une redirection invalide', async () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const service = TestBed.inject(EntraAuthService);
    const jetons = TestBed.inject(AuthTokenService);

    await expect(service.connecterEtRevenirA('/diagnostic')).resolves.toBeUndefined();
    expect(jetons.isAuthenticated()).toBe(false);
  });

  it('`renouvelerJeton` et `obtenirJetonFrais` restent inoffensifs sans client MSAL', async () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const service = TestBed.inject(EntraAuthService);

    await expect(service.renouvelerJeton()).resolves.toBeNull();
    await expect(service.obtenirJetonFrais()).resolves.toBe('');
  });
});
