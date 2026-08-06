import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { EntraAuthService } from '../auth/entra-auth.service';
import { sessionRenewalInterceptor } from './session-renewal.interceptor';

describe('sessionRenewalInterceptor', () => {
  let http: HttpClient;
  let controleur: HttpTestingController;
  let renouveler: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    renouveler = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([sessionRenewalInterceptor])),
        provideHttpClientTesting(),
        // Double minimale : seule `renouvelerJeton` est appelée par l'intercepteur.
        { provide: EntraAuthService, useValue: { renouvelerJeton: renouveler } },
      ],
    });

    http = TestBed.inject(HttpClient);
    controleur = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controleur.verify();
    vi.restoreAllMocks();
  });

  it('laisse passer une requête hors API sans jamais consulter le jeton', () => {
    http.get('https://exemple.test/ressource').subscribe();

    controleur.expectOne('https://exemple.test/ressource').flush({});

    expect(renouveler).not.toHaveBeenCalled();
  });

  it('rejoue une requête API en 401 après renouvellement, avec le jeton frais', async () => {
    renouveler.mockResolvedValue('jeton-frais');
    let reponse: unknown;
    http.get('/api/tickets').subscribe((valeur) => (reponse = valeur));

    controleur
      .expectOne('/api/tickets')
      .flush(
        { title: 'Non authentifié', status: 401 },
        { status: 401, statusText: 'Unauthorized' },
      );

    // Le renouvellement est asynchrone : laisse la micro-tâche de `switchMap` s'exécuter.
    await Promise.resolve();
    await Promise.resolve();

    const rejeu = controleur.expectOne('/api/tickets');
    expect(rejeu.request.headers.get('Authorization')).toBe('Bearer jeton-frais');
    rejeu.flush([{ id: 't1' }]);

    expect(reponse).toEqual([{ id: 't1' }]);
  });

  it('propage le 401 inchangé quand le renouvellement échoue, sans rejeu', async () => {
    renouveler.mockResolvedValue(null);
    let erreur: unknown;
    http.get('/api/tickets').subscribe({ error: (valeur) => (erreur = valeur) });

    controleur
      .expectOne('/api/tickets')
      .flush(
        { title: 'Non authentifié', status: 401 },
        { status: 401, statusText: 'Unauthorized' },
      );

    await Promise.resolve();
    await Promise.resolve();

    expect(erreur).toMatchObject({ status: 401 });
    controleur.expectNone('/api/tickets');
  });

  it('ne rejoue jamais un 403 : ce n’est pas un jeton périmé qui est en cause', () => {
    let erreur: unknown;
    http.get('/api/tickets').subscribe({ error: (valeur) => (erreur = valeur) });

    controleur
      .expectOne('/api/tickets')
      .flush({ title: 'Interdit', status: 403 }, { status: 403, statusText: 'Forbidden' });

    expect(erreur).toMatchObject({ status: 403 });
    expect(renouveler).not.toHaveBeenCalled();
  });

  it('ne rejoue qu’une fois : un second 401 après rejeu remonte tel quel', async () => {
    renouveler.mockResolvedValue('jeton-toujours-refuse');
    let erreur: unknown;
    http.get('/api/tickets').subscribe({ error: (valeur) => (erreur = valeur) });

    controleur
      .expectOne('/api/tickets')
      .flush(
        { title: 'Non authentifié', status: 401 },
        { status: 401, statusText: 'Unauthorized' },
      );

    await Promise.resolve();
    await Promise.resolve();

    controleur
      .expectOne('/api/tickets')
      .flush(
        { title: 'Non authentifié', status: 401 },
        { status: 401, statusText: 'Unauthorized' },
      );

    expect(erreur).toMatchObject({ status: 401 });
    expect(renouveler).toHaveBeenCalledTimes(1);
  });
});
