import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AssetResponse, RegisterAssetRequest } from '../../shared/models/asset.model';
import { AssetsApiService } from './assets-api.service';

const ACTIF: AssetResponse = {
  id: '11111111-1111-1111-1111-111111111111',
  name: 'Serveur de sauvegarde',
  serialNumber: 'SRV-00042',
  type: 'Server',
  status: 'InService',
  createdAt: '2026-08-05T09:00:00Z',
};

describe('AssetsApiService', () => {
  let service: AssetsApiService;
  let controleur: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(AssetsApiService);
    controleur = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controleur.verify());

  it("liste l'inventaire — GET /api/assets", () => {
    let recu: readonly AssetResponse[] | undefined;
    service.getAll().subscribe((actifs) => (recu = actifs));

    const requete = controleur.expectOne('/api/assets');
    expect(requete.request.method).toBe('GET');
    requete.flush([ACTIF]);

    expect(recu).toEqual([ACTIF]);
  });

  it("lit la fiche d'un actif — GET /api/assets/{id}", () => {
    service.getById(ACTIF.id).subscribe();

    const requete = controleur.expectOne(`/api/assets/${ACTIF.id}`);
    expect(requete.request.method).toBe('GET');
    requete.flush({ ...ACTIF, tickets: [] });
  });

  it('enregistre un actif — POST /api/assets', () => {
    const demande: RegisterAssetRequest = {
      name: 'Portable RH',
      serialNumber: 'LAP-00099',
      type: 'Laptop',
    };

    service.register(demande).subscribe();

    const requete = controleur.expectOne('/api/assets');
    expect(requete.request.method).toBe('POST');
    expect(requete.request.body).toEqual(demande);
    requete.flush(ACTIF, { status: 201, statusText: 'Created' });
  });

  it('met un actif au rebut — PUT /api/assets/{id}/decommission', () => {
    service.decommission(ACTIF.id).subscribe();

    const requete = controleur.expectOne(`/api/assets/${ACTIF.id}/decommission`);
    expect(requete.request.method).toBe('PUT');
    expect(requete.request.body).toBeNull();
    requete.flush(null, { status: 204, statusText: 'No Content' });
  });
});
