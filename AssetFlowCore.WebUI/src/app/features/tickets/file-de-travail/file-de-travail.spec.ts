import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { errorInterceptor } from '../../../core/http/error.interceptor';
import { PagedResult } from '../../../shared/models/paged-result.model';
import { TicketResponse } from '../../../shared/models/ticket.model';
import { FileDeTravail } from './file-de-travail';

function page(
  items: readonly TicketResponse[],
  total = items.length,
  numeroPage = 1,
): PagedResult<TicketResponse> {
  return {
    items,
    page: numeroPage,
    pageSize: 20,
    totalCount: total,
    totalPages: Math.ceil(total / 20) || 0,
  };
}

const TICKET: TicketResponse = {
  id: '11111111-1111-1111-1111-111111111111',
  assetId: '22222222-2222-2222-2222-222222222222',
  title: 'Ventilateur bruyant',
  description: 'Bruit anormal détecté',
  criticality: 'High',
  status: 'Opened',
  assignedTeamId: '33333333-3333-3333-3333-333333333333',
  assignedTeamName: 'Équipe Serveurs Critiques',
  resolutionComment: null,
  createdAt: '2026-08-05T09:00:00Z',
  assistanceNote: null,
  isAiProcessing: true,
  assignedByUserId: null,
  closedByUserId: null,
  transferHistory: [],
};

describe('FileDeTravail', () => {
  let controleur: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [FileDeTravail],
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });

    controleur = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controleur.verify());

  function creer(): ComponentFixture<FileDeTravail> {
    const fixture = TestBed.createComponent(FileDeTravail);
    TestBed.tick();
    return fixture;
  }

  function element(fixture: ComponentFixture<FileDeTravail>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function texte(fixture: ComponentFixture<FileDeTravail>): string {
    return element(fixture).textContent ?? '';
  }

  /** La première requête part sans aucun filtre : uniquement `page=1&pageSize=20`. */
  function requeteInitiale() {
    return controleur.expectOne(
      (r) =>
        r.url === '/api/v1/tickets' &&
        r.params.get('page') === '1' &&
        r.params.get('pageSize') === '20' &&
        r.params.get('status') === null &&
        r.params.get('criticality') === null,
    );
  }

  it('annonce le chargement avant la réponse de l’API', () => {
    const fixture = creer();

    expect(texte(fixture)).toContain('Chargement');
    requeteInitiale().flush(page([]));
  });

  it("affiche l'état vide quand aucun incident ne correspond", async () => {
    const fixture = creer();
    requeteInitiale().flush(page([]));
    await fixture.whenStable();

    expect(texte(fixture)).toContain('Aucun incident ne correspond');
  });

  it('affiche une erreur normalisée sans le détail technique', async () => {
    const fixture = creer();

    requeteInitiale().flush(
      { title: 'Erreur interne du serveur', status: 500, detail: 'Détail technique interne.' },
      { status: 500, statusText: 'Internal Server Error' },
    );
    await fixture.whenStable();

    expect(texte(fixture)).not.toContain('Détail technique interne.');
    expect(element(fixture).querySelector('[role="alert"]')).not.toBeNull();
  });

  it('affiche la file reçue avec un lien vers la fiche de chaque incident', async () => {
    const fixture = creer();
    requeteInitiale().flush(page([TICKET]));
    await fixture.whenStable();

    const rendu = texte(fixture);
    expect(rendu).toContain('Ventilateur bruyant');
    expect(rendu).toContain('Équipe Serveurs Critiques');

    const lien = element(fixture).querySelector<HTMLAnchorElement>(
      `a[href="/tickets/${TICKET.id}"]`,
    );
    expect(lien?.textContent?.trim()).toBe(TICKET.title);
  });

  it('relance la recherche filtrée par état et revient à la page 1', async () => {
    const fixture = creer();
    requeteInitiale().flush(page([TICKET], 45));
    await fixture.whenStable();

    const champs = [...element(fixture).querySelectorAll<HTMLSelectElement>('select')];
    const champStatut = champs.find(
      (candidat) =>
        element(fixture).querySelector(`label[for="${candidat.id}"]`)?.textContent?.trim() ===
        'État',
    );
    if (champStatut === undefined) {
      throw new Error('Champ de filtre "État" introuvable.');
    }
    champStatut.value = 'Closed';
    champStatut.dispatchEvent(new Event('change'));
    // Filtrage délégué au serveur (à la différence de l'inventaire, filtré côté client) : le
    // changement déclenche une nouvelle requête, qu'il faut laisser partir puis honorer avant
    // `whenStable()` — sans quoi la requête pendante ferait attendre indéfiniment.
    TestBed.tick();

    const requete = controleur.expectOne(
      (r) => r.url === '/api/v1/tickets' && r.params.get('status') === 'Closed',
    );
    expect(requete.request.params.get('page')).toBe('1');
    requete.flush(page([]));
    await fixture.whenStable();
  });

  it('navigue vers la page suivante puis précédente', async () => {
    const fixture = creer();
    requeteInitiale().flush(page([TICKET], 45));
    await fixture.whenStable();

    expect(texte(fixture)).toContain('Page 1 sur 3');

    /**
     * Reinterrogé à chaque fois plutôt que capturé une fois : chaque changement de page traverse
     * brièvement l'état « chargement » (spinner à la place du tableau et de la pagination), qui
     * démonte puis remonte les boutons — une référence capturée avant ce cycle pointerait vers un
     * nœud détaché, dont le clic ne déclencherait plus rien.
     */
    function bouton(libelle: string): HTMLButtonElement | undefined {
      return [...element(fixture).querySelectorAll<HTMLButtonElement>('button')].find(
        (b) => b.getAttribute('aria-label') === libelle,
      );
    }

    bouton('Page suivante')?.click();
    TestBed.tick();

    controleur.expectOne((r) => r.params.get('page') === '2').flush(page([TICKET], 45, 2));
    await fixture.whenStable();

    expect(texte(fixture)).toContain('Page 2 sur 3');

    bouton('Page précédente')?.click();
    TestBed.tick();

    controleur.expectOne((r) => r.params.get('page') === '1').flush(page([TICKET], 45, 1));
    await fixture.whenStable();
  });
});
