import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HubConnection } from '@microsoft/signalr';
import { vi } from 'vitest';
import { HUB_CONNECTION_FACTORY } from '../../core/realtime/ticket-hub.service';
import { errorInterceptor } from '../../core/http/error.interceptor';
import { AssetResponse } from '../../shared/models/asset.model';
import { Diagnostic } from './diagnostic';

const ACTIF: AssetResponse = {
  id: '11111111-1111-1111-1111-111111111111',
  name: 'Serveur de sauvegarde',
  serialNumber: 'SRV-00042',
  type: 'Server',
  status: 'InService',
  createdAt: '2026-08-05T09:00:00Z',
};

describe('Diagnostic', () => {
  let controleur: HttpTestingController;

  beforeEach(() => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);

    TestBed.configureTestingModule({
      imports: [Diagnostic],
      providers: [
        // La chaîne réelle est reproduite, interceptor d'erreurs compris : c'est lui qui
        // fournit le message affiché en cas d'échec.
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        // Aucune connexion réelle : la fabrique n'est de toute façon appelée qu'à la demande.
        { provide: HUB_CONNECTION_FACTORY, useValue: () => ({}) as unknown as HubConnection },
      ],
    });

    controleur = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controleur.verify();
    vi.restoreAllMocks();
  });

  /** Crée le composant et laisse partir l'appel émis par la ressource. */
  function creer(): ComponentFixture<Diagnostic> {
    const fixture = TestBed.createComponent(Diagnostic);
    TestBed.tick();
    return fixture;
  }

  function texte(fixture: ComponentFixture<Diagnostic>): string {
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  it("annonce le chargement avant la réponse de l'API", () => {
    const fixture = creer();

    expect(texte(fixture)).toContain("Interrogation de l'API");
    controleur.expectOne('/api/v1/assets').flush([]);
  });

  it("affiche l'inventaire reçu", async () => {
    const fixture = creer();

    controleur.expectOne('/api/v1/assets').flush([ACTIF]);
    await fixture.whenStable();

    expect(texte(fixture)).toContain('1 actif(s)');
    expect(texte(fixture)).toContain('SRV-00042');
  });

  it("distingue l'état vide d'une absence de réponse", async () => {
    const fixture = creer();

    controleur.expectOne('/api/v1/assets').flush([]);
    await fixture.whenStable();

    expect(texte(fixture)).toContain('0 actif(s)');
    expect(texte(fixture)).toContain("l'API a répondu, l'inventaire est vide");
  });

  it('affiche le message normalisé et le traceId sur une erreur serveur', async () => {
    const fixture = creer();

    controleur.expectOne('/api/v1/assets').flush(
      {
        title: 'Erreur interne du serveur',
        status: 500,
        detail: 'Détail technique interne.',
        traceId: '0HN7ABCDEF:00000003',
      },
      { status: 500, statusText: 'Internal Server Error' },
    );
    await fixture.whenStable();

    const rendu = texte(fixture);
    expect(rendu).toContain('0HN7ABCDEF:00000003');
    expect(rendu).not.toContain('Détail technique interne.');
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).not.toBeNull();
  });

  it("relance l'appel à la demande après un échec", async () => {
    const fixture = creer();

    controleur
      .expectOne('/api/v1/assets')
      .flush({ title: 'Erreur', status: 500 }, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    const bouton = (fixture.nativeElement as HTMLElement).querySelector('button');
    bouton?.click();
    TestBed.tick();

    controleur.expectOne('/api/v1/assets').flush([ACTIF]);
    await fixture.whenStable();

    expect(texte(fixture)).toContain('1 actif(s)');
  });

  it('affiche la liaison temps réel comme déconnectée au chargement', () => {
    const fixture = creer();

    expect(texte(fixture)).toContain('disconnected');
    expect(texte(fixture)).toContain('Aucun incident reçu');
    controleur.expectOne('/api/v1/assets').flush([]);
  });
});
