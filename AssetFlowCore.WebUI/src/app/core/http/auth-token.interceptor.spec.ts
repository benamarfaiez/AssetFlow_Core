import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthTokenService } from '../auth/auth-token.service';
import { authTokenInterceptor } from './auth-token.interceptor';

describe('authTokenInterceptor', () => {
  let http: HttpClient;
  let controleur: HttpTestingController;
  let jetons: AuthTokenService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authTokenInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    controleur = TestBed.inject(HttpTestingController);
    jetons = TestBed.inject(AuthTokenService);
  });

  afterEach(() => controleur.verify());

  it("n'ajoute aucun en-tête en l'absence de jeton — l'état du Lot 3", () => {
    http.get('/api/assets').subscribe();

    const requete = controleur.expectOne('/api/assets');
    expect(requete.request.headers.has('Authorization')).toBe(false);
    requete.flush([]);
  });

  it("attache le jeton aux appels de l'API dès qu'il en existe un (Lot 7)", () => {
    jetons.store('jeton-de-test');

    http.get('/api/tickets').subscribe();

    const requete = controleur.expectOne('/api/tickets');
    expect(requete.request.headers.get('Authorization')).toBe('Bearer jeton-de-test');
    requete.flush({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
  });

  it("n'attache jamais le jeton à une requête sortant hors de l'API", () => {
    jetons.store('jeton-de-test');

    http.get('https://exemple.test/ressource').subscribe();

    const requete = controleur.expectOne('https://exemple.test/ressource');
    expect(requete.request.headers.has('Authorization')).toBe(false);
    requete.flush({});
  });

  it('cesse de joindre le jeton après une déconnexion', () => {
    jetons.store('jeton-de-test');
    jetons.clear();

    http.get('/api/teams').subscribe();

    const requete = controleur.expectOne('/api/teams');
    expect(requete.request.headers.has('Authorization')).toBe(false);
    expect(jetons.isAuthenticated()).toBe(false);
    requete.flush([]);
  });
});
