import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { ApiError } from '../../shared/models/api-error.model';
import { errorInterceptor } from './error.interceptor';

const URL_APPEL = '/api/assets';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let controleur: HttpTestingController;

  beforeEach(() => {
    // La journalisation de développement des 5xx et des coupures réseau est neutralisée :
    // elle est attendue, mais elle brouillerait la sortie des tests.
    vi.spyOn(console, 'error').mockImplementation(() => undefined);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    controleur = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controleur.verify();
    vi.restoreAllMocks();
  });

  /** Déclenche un appel et rend l'erreur normalisée qu'il produit. */
  function appelerEtCapturer(): { lire: () => ApiError } {
    let capturee: ApiError | undefined;
    http.get(URL_APPEL).subscribe({ error: (erreur: ApiError) => (capturee = erreur) });

    return {
      lire: () => {
        if (capturee === undefined) {
          throw new Error("Aucune erreur n'a été reçue par l'abonné.");
        }
        return capturee;
      },
    };
  }

  it('traduit un 400 de validation en messages reportables sur les champs', () => {
    const appel = appelerEtCapturer();

    controleur.expectOne(URL_APPEL).flush(
      {
        title: 'Validation de la requête échouée',
        status: 400,
        detail: 'Une ou plusieurs erreurs de validation se sont produites.',
        // Clés en PascalCase, telles que produites par FluentValidation.
        errors: {
          SerialNumber: ['Le numéro de série doit contenir entre 5 et 50 caractères.'],
          'Command.Name': ["Le nom de l'actif est obligatoire."],
        },
      },
      { status: 400, statusText: 'Bad Request' },
    );

    const erreur = appel.lire();
    expect(erreur).toBeInstanceOf(ApiError);
    expect(erreur.kind).toBe('validation');
    expect(erreur.status).toBe(400);
    expect(erreur.hasFieldErrors).toBe(true);
    // Clés converties en camelCase, et clé composée réduite à son dernier segment.
    expect(erreur.messagesFor('serialNumber')).toEqual([
      'Le numéro de série doit contenir entre 5 et 50 caractères.',
    ]);
    expect(erreur.messagesFor('name')).toEqual(["Le nom de l'actif est obligatoire."]);
    expect(erreur.messagesFor('inconnu')).toEqual([]);
  });

  it('traduit un 400 sans dictionnaire en refus métier, en conservant le message du backend', () => {
    const appel = appelerEtCapturer();

    controleur.expectOne(URL_APPEL).flush(
      {
        title: 'Règle métier violée',
        status: 400,
        detail: "Action interdite : l'actif fait l'objet de 2 incident(s) en cours de traitement.",
      },
      { status: 400, statusText: 'Bad Request' },
    );

    const erreur = appel.lire();
    expect(erreur.kind).toBe('business');
    expect(erreur.message).toBe(
      "Action interdite : l'actif fait l'objet de 2 incident(s) en cours de traitement.",
    );
    expect(erreur.hasFieldErrors).toBe(false);
  });

  it('traduit un 401 en session expirée ou invalide', () => {
    const appel = appelerEtCapturer();

    controleur
      .expectOne(URL_APPEL)
      .flush(
        { title: 'Non authentifié', status: 401 },
        { status: 401, statusText: 'Unauthorized' },
      );

    const erreur = appel.lire();
    expect(erreur.kind).toBe('unauthorized');
    expect(erreur.status).toBe(401);
    expect(erreur.message).toContain('session');
  });

  it('traduit un 403 en autorisation refusée', () => {
    const appel = appelerEtCapturer();

    controleur
      .expectOne(URL_APPEL)
      .flush({ title: 'Interdit', status: 403 }, { status: 403, statusText: 'Forbidden' });

    const erreur = appel.lire();
    expect(erreur.kind).toBe('forbidden');
    expect(erreur.status).toBe(403);
    expect(erreur.hasFieldErrors).toBe(false);
  });

  it('traduit un 404 en ressource introuvable', () => {
    const appel = appelerEtCapturer();

    controleur
      .expectOne(URL_APPEL)
      .flush(
        { title: 'Ressource introuvable', status: 404, detail: "L'actif demandé n'existe pas." },
        { status: 404, statusText: 'Not Found' },
      );

    const erreur = appel.lire();
    expect(erreur.kind).toBe('notFound');
    expect(erreur.message).toBe("L'actif demandé n'existe pas.");
  });

  it('traduit un 409 en conflit de concurrence', () => {
    const appel = appelerEtCapturer();

    controleur.expectOne(URL_APPEL).flush(
      {
        title: "Concurrence d'accès détectée",
        status: 409,
        detail:
          'Cette ressource a été mise à jour par un autre utilisateur. Veuillez recharger les données.',
      },
      { status: 409, statusText: 'Conflict' },
    );

    const erreur = appel.lire();
    expect(erreur.kind).toBe('conflict');
    expect(erreur.message).toContain('recharger');
  });

  it('traduit un 500 en message générique, sans divulguer le détail, et retient le traceId', () => {
    const appel = appelerEtCapturer();

    controleur.expectOne(URL_APPEL).flush(
      {
        title: 'Erreur interne du serveur',
        status: 500,
        detail: 'Détail technique qui ne doit jamais être présenté à un utilisateur.',
        traceId: '0HN7ABCDEF:00000003',
      },
      { status: 500, statusText: 'Internal Server Error' },
    );

    const erreur = appel.lire();
    expect(erreur.kind).toBe('server');
    expect(erreur.message).not.toContain('Détail technique');
    expect(erreur.traceId).toBe('0HN7ABCDEF:00000003');
    // Le corps d'origine reste disponible pour le diagnostic.
    expect(erreur.problemDetails?.detail).toContain('Détail technique');
  });

  it("distingue une absence de réponse (serveur injoignable) d'une erreur serveur", () => {
    const appel = appelerEtCapturer();

    controleur.expectOne(URL_APPEL).error(new ProgressEvent('error'));

    const erreur = appel.lire();
    expect(erreur.kind).toBe('network');
    expect(erreur.status).toBe(0);
    expect(erreur.problemDetails).toBeNull();
  });

  it('traite un corps non JSON sans échouer', () => {
    const appel = appelerEtCapturer();

    controleur
      .expectOne(URL_APPEL)
      .flush('<html>502 Bad Gateway</html>', { status: 502, statusText: 'Bad Gateway' });

    const erreur = appel.lire();
    expect(erreur.kind).toBe('server');
    expect(erreur.status).toBe(502);
    expect(erreur.problemDetails).toBeNull();
  });

  it('laisse passer une réponse en succès sans la modifier', () => {
    let recu: unknown;
    http.get(URL_APPEL).subscribe((reponse) => (recu = reponse));

    controleur.expectOne(URL_APPEL).flush([{ id: 'a1' }]);

    expect(recu).toEqual([{ id: 'a1' }]);
  });
});
