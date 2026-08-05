import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TeamResponse } from '../../shared/models/team.model';
import { TeamsApiService } from './teams-api.service';

const EQUIPE: TeamResponse = {
  id: '33333333-3333-3333-3333-333333333333',
  name: 'Équipe Serveurs Critiques',
  description: 'Astreinte sur les serveurs de production',
  isActive: true,
  createdAt: '2026-08-05T08:00:00Z',
  assetType: 'Server',
  ticketCriticality: 'High',
};

describe('TeamsApiService', () => {
  let service: TeamsApiService;
  let controleur: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(TeamsApiService);
    controleur = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controleur.verify());

  it('liste toutes les équipes par défaut, y compris les désactivées', () => {
    let recu: readonly TeamResponse[] | undefined;
    service.getAll().subscribe((equipes) => (recu = equipes));

    const requete = controleur.expectOne((r) => r.url === '/api/teams');
    expect(requete.request.method).toBe('GET');
    expect(requete.request.params.get('onlyActive')).toBe('false');
    requete.flush([EQUIPE]);

    expect(recu).toEqual([EQUIPE]);
  });

  it('restreint la liste aux équipes actives à la demande — le cas du sélecteur de transfert', () => {
    service.getAll(true).subscribe();

    const requete = controleur.expectOne((r) => r.url === '/api/teams');
    expect(requete.request.params.get('onlyActive')).toBe('true');
    requete.flush([EQUIPE]);
  });

  it("lit la fiche d'une équipe — GET /api/teams/{id}", () => {
    service.getById(EQUIPE.id).subscribe();

    const requete = controleur.expectOne(`/api/teams/${EQUIPE.id}`);
    expect(requete.request.method).toBe('GET');
    requete.flush(EQUIPE);
  });

  it('crée une équipe — POST /api/teams', () => {
    const demande = {
      name: 'Équipe Réseau',
      assetType: 'NetworkDevice',
      ticketCriticality: 'Medium',
      description: null,
    } as const;

    service.create(demande).subscribe();

    const requete = controleur.expectOne('/api/teams');
    expect(requete.request.method).toBe('POST');
    expect(requete.request.body).toEqual(demande);
    requete.flush(EQUIPE, { status: 201, statusText: 'Created' });
  });

  it('met à jour une équipe et attend un 200 — PUT /api/teams/{id}', () => {
    let recu: TeamResponse | undefined;
    service.update(EQUIPE.id, { description: 'Astreinte élargie' }).subscribe((e) => (recu = e));

    const requete = controleur.expectOne(`/api/teams/${EQUIPE.id}`);
    expect(requete.request.method).toBe('PUT');
    expect(requete.request.body).toEqual({ description: 'Astreinte élargie' });
    requete.flush(EQUIPE, { status: 200, statusText: 'OK' });

    expect(recu).toEqual(EQUIPE);
  });

  it('supprime une équipe — DELETE /api/teams/{id}', () => {
    service.delete(EQUIPE.id).subscribe();

    const requete = controleur.expectOne(`/api/teams/${EQUIPE.id}`);
    expect(requete.request.method).toBe('DELETE');
    requete.flush(null, { status: 204, statusText: 'No Content' });
  });
});
