import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PagedResult } from '../../shared/models/paged-result.model';
import { TicketResponse } from '../../shared/models/ticket.model';
import { TicketsApiService } from './tickets-api.service';

const INCIDENT: TicketResponse = {
  id: '22222222-2222-2222-2222-222222222222',
  assetId: '11111111-1111-1111-1111-111111111111',
  title: 'Ventilateur bruyant',
  description: "Bruit continu depuis l'arrêt du climatiseur.",
  criticality: 'High',
  status: 'Opened',
  assignedTeamId: '33333333-3333-3333-3333-333333333333',
  assignedTeamName: 'Équipe Serveurs Critiques',
  resolutionComment: null,
  createdAt: '2026-08-05T09:30:00Z',
  assistanceNote: null,
  isAiProcessing: true,
};

describe('TicketsApiService', () => {
  let service: TicketsApiService;
  let controleur: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(TicketsApiService);
    controleur = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controleur.verify());

  it("ne transmet aucun paramètre quand aucun filtre n'est fourni : les défauts du backend s'appliquent", () => {
    let recu: PagedResult<TicketResponse> | undefined;
    service.search().subscribe((page) => (recu = page));

    const requete = controleur.expectOne((r) => r.url === '/api/tickets');
    expect(requete.request.method).toBe('GET');
    expect(requete.request.params.keys()).toEqual([]);
    requete.flush({ items: [INCIDENT], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 });

    expect(recu?.totalCount).toBe(1);
  });

  it('transmet les filtres, le tri et la pagination fournis', () => {
    service
      .search({
        status: 'InProgress',
        criticality: 'High',
        teamId: '33333333-3333-3333-3333-333333333333',
        assetId: '11111111-1111-1111-1111-111111111111',
        sortBy: 'Criticality',
        sortDescending: false,
        page: 2,
        pageSize: 50,
      })
      .subscribe();

    const requete = controleur.expectOne((r) => r.url === '/api/tickets');
    const params = requete.request.params;
    expect(params.get('status')).toBe('InProgress');
    expect(params.get('criticality')).toBe('High');
    expect(params.get('teamId')).toBe('33333333-3333-3333-3333-333333333333');
    expect(params.get('assetId')).toBe('11111111-1111-1111-1111-111111111111');
    expect(params.get('sortBy')).toBe('Criticality');
    expect(params.get('sortDescending')).toBe('false');
    expect(params.get('page')).toBe('2');
    expect(params.get('pageSize')).toBe('50');
    requete.flush({ items: [], page: 2, pageSize: 50, totalCount: 0, totalPages: 0 });
  });

  it('lit un incident — GET /api/tickets/{id}', () => {
    service.getById(INCIDENT.id).subscribe();

    const requete = controleur.expectOne(`/api/tickets/${INCIDENT.id}`);
    expect(requete.request.method).toBe('GET');
    requete.flush(INCIDENT);
  });

  it('ouvre un incident — POST /api/tickets', () => {
    const demande = {
      assetId: INCIDENT.assetId,
      title: INCIDENT.title,
      description: INCIDENT.description,
      criticality: 'High',
    } as const;

    service.create(demande).subscribe();

    const requete = controleur.expectOne('/api/tickets');
    expect(requete.request.method).toBe('POST');
    expect(requete.request.body).toEqual(demande);
    requete.flush(INCIDENT, { status: 201, statusText: 'Created' });
  });

  it('prend en charge un incident sans corps — PUT /api/tickets/{id}/assign', () => {
    service.assign(INCIDENT.id).subscribe();

    const requete = controleur.expectOne(`/api/tickets/${INCIDENT.id}/assign`);
    expect(requete.request.method).toBe('PUT');
    expect(requete.request.body).toBeNull();
    requete.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('clôture un incident avec son compte rendu — PUT /api/tickets/{id}/close', () => {
    service.close(INCIDENT.id, { resolutionComment: 'Ventilateur remplacé.' }).subscribe();

    const requete = controleur.expectOne(`/api/tickets/${INCIDENT.id}/close`);
    expect(requete.request.method).toBe('PUT');
    expect(requete.request.body).toEqual({ resolutionComment: 'Ventilateur remplacé.' });
    requete.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('transfère un incident vers une équipe désignée par son nom — POST /api/tickets/{id}/transfer', () => {
    service
      .transfer(INCIDENT.id, { targetTeam: 'Équipe Réseau', reason: 'Panne de commutateur' })
      .subscribe();

    const requete = controleur.expectOne(`/api/tickets/${INCIDENT.id}/transfer`);
    expect(requete.request.method).toBe('POST');
    expect(requete.request.body).toEqual({
      targetTeam: 'Équipe Réseau',
      reason: 'Panne de commutateur',
    });
    requete.flush(null, { status: 204, statusText: 'No Content' });
  });
});
