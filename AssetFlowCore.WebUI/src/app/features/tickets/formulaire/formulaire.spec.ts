import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { errorInterceptor } from '../../../core/http/error.interceptor';
import { AssetResponse } from '../../../shared/models/asset.model';
import { TicketResponse } from '../../../shared/models/ticket.model';
import { Formulaire } from './formulaire';

const ACTIF_EN_SERVICE: AssetResponse = {
  id: '11111111-1111-1111-1111-111111111111',
  name: 'Serveur de sauvegarde',
  serialNumber: 'SRV-00042',
  type: 'Server',
  status: 'InService',
  createdAt: '2026-08-05T09:00:00Z',
};

const ACTIF_AU_REBUT: AssetResponse = {
  id: '22222222-2222-2222-2222-222222222222',
  name: 'Portable retiré',
  serialNumber: 'LAP-00001',
  type: 'Laptop',
  status: 'Decommissioned',
  createdAt: '2026-08-01T09:00:00Z',
};

const INCIDENT_CREE: TicketResponse = {
  id: '33333333-3333-3333-3333-333333333333',
  assetId: ACTIF_EN_SERVICE.id,
  title: 'Ventilateur bruyant',
  description: 'Bruit anormal',
  criticality: 'High',
  status: 'Opened',
  assignedTeamId: '44444444-4444-4444-4444-444444444444',
  assignedTeamName: 'Équipe Serveurs Critiques',
  resolutionComment: null,
  createdAt: '2026-08-08T09:00:00Z',
  assistanceNote: null,
  isAiProcessing: true,
  assignedByUserId: null,
  closedByUserId: null,
  transferHistory: [],
};

describe('Formulaire (tickets)', () => {
  let controleur: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Formulaire],
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });

    controleur = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controleur.verify());

  function element(fixture: ComponentFixture<Formulaire>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  /** Crée le composant, répond à la résolution des actifs, laisse le rendu se stabiliser. */
  async function creer(
    actifs: readonly AssetResponse[] = [ACTIF_EN_SERVICE, ACTIF_AU_REBUT],
    assetId?: string,
  ): Promise<ComponentFixture<Formulaire>> {
    const fixture = TestBed.createComponent(Formulaire);
    if (assetId !== undefined) {
      fixture.componentRef.setInput('assetId', assetId);
    }
    fixture.detectChanges();

    controleur.expectOne('/api/v1/assets').flush(actifs);
    await fixture.whenStable();
    // `afterNextRender` (pré-remplissage de l'actif) : un rendu supplémentaire pour s'exécuter.
    await fixture.whenStable();

    return fixture;
  }

  function champ(fixture: ComponentFixture<Formulaire>, label: string): HTMLElement {
    const trouve = [...element(fixture).querySelectorAll<HTMLLabelElement>('label')].find(
      (candidat) => candidat.textContent?.trim().startsWith(label),
    );
    if (trouve === undefined) {
      throw new Error(`Champ "${label}" introuvable.`);
    }
    const id = trouve.getAttribute('for');
    const controle = element(fixture).querySelector<HTMLElement>(`#${id}`);
    if (controle === null) {
      throw new Error(`Contrôle du champ "${label}" introuvable (id ${id}).`);
    }
    return controle;
  }

  function saisir(fixture: ComponentFixture<Formulaire>, label: string, valeur: string): void {
    const input = champ(fixture, label) as HTMLInputElement | HTMLTextAreaElement;
    input.value = valeur;
    input.dispatchEvent(new Event('input', { bubbles: true }));
  }

  function selectionner(
    fixture: ComponentFixture<Formulaire>,
    label: string,
    valeur: string,
  ): void {
    const select = champ(fixture, label) as HTMLSelectElement;
    select.value = valeur;
    select.dispatchEvent(new Event('change'));
  }

  async function soumettre(fixture: ComponentFixture<Formulaire>): Promise<void> {
    const bouton = element(fixture).querySelector<HTMLButtonElement>('button[type="submit"]');
    if (bouton === null) {
      throw new Error('Bouton de soumission introuvable.');
    }
    bouton.click();
    await fixture.whenStable();
  }

  it('affiche une erreur et aucun formulaire si le chargement des équipements échoue', async () => {
    const fixture = TestBed.createComponent(Formulaire);
    fixture.detectChanges();

    controleur
      .expectOne('/api/v1/assets')
      .flush(
        { title: 'Erreur serveur', status: 500, traceId: 'trace-abc-123' },
        { status: 500, statusText: 'Internal Server Error' },
      );
    await fixture.whenStable();

    expect(element(fixture).querySelector('[role="alert"]')).not.toBeNull();
    expect(element(fixture).querySelector('form')).toBeNull();
  });

  it("affiche un message dédié et aucun formulaire quand aucun équipement n'est disponible", async () => {
    const fixture = await creer([]);

    expect(element(fixture).textContent).toContain(
      "Aucun équipement disponible pour l'ouverture d'un incident.",
    );
    expect(element(fixture).querySelector('form')).toBeNull();
  });

  it('exclut les actifs déjà au rebut du sélecteur (RM-09)', async () => {
    const fixture = await creer();

    const rendu = element(fixture).textContent ?? '';
    expect(rendu).toContain('Serveur de sauvegarde');
    expect(rendu).not.toContain('Portable retiré');
  });

  it('pré-remplit l’actif quand `assetId` est fourni (arrivée depuis la fiche d’un actif)', async () => {
    const fixture = await creer(undefined, ACTIF_EN_SERVICE.id);

    const select = champ(fixture, 'Équipement concerné') as HTMLSelectElement;
    expect(select.value).toBe(ACTIF_EN_SERVICE.id);
  });

  it('affiche les erreurs de champs sans aucun appel réseau sur un formulaire vide', async () => {
    const fixture = await creer();

    await soumettre(fixture);

    controleur.expectNone((r) => r.method === 'POST');
    expect(element(fixture).textContent).toContain('Ce champ est obligatoire');
  });

  it('refuse localement un titre de plus de 150 caractères, sans appel réseau', async () => {
    const fixture = await creer();

    selectionner(fixture, 'Équipement concerné', ACTIF_EN_SERVICE.id);
    saisir(fixture, 'Titre', 'x'.repeat(151));
    saisir(fixture, 'Description', 'Une description valide.');
    selectionner(fixture, 'Criticité', 'High');

    await soumettre(fixture);

    controleur.expectNone((r) => r.method === 'POST');
  });

  it('ouvre l’incident avec succès et navigue vers sa fiche', async () => {
    const fixture = await creer();

    selectionner(fixture, 'Équipement concerné', ACTIF_EN_SERVICE.id);
    saisir(fixture, 'Titre', 'Ventilateur bruyant');
    saisir(fixture, 'Description', 'Bruit anormal');
    selectionner(fixture, 'Criticité', 'High');

    const router = TestBed.inject(Router);
    const navigation = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    await soumettre(fixture);

    const requete = controleur.expectOne((r) => r.method === 'POST');
    expect(requete.request.body).toEqual({
      assetId: ACTIF_EN_SERVICE.id,
      title: 'Ventilateur bruyant',
      description: 'Bruit anormal',
      criticality: 'High',
    });

    requete.flush(INCIDENT_CREE, { status: 201, statusText: 'Created' });
    await fixture.whenStable();

    expect(navigation).toHaveBeenCalledWith(['/tickets', INCIDENT_CREE.id]);
  });

  it('affiche une anomalie de configuration du référentiel, distincte d’une erreur de saisie (RM-12)', async () => {
    const fixture = await creer();

    selectionner(fixture, 'Équipement concerné', ACTIF_EN_SERVICE.id);
    saisir(fixture, 'Titre', 'Ventilateur bruyant');
    saisir(fixture, 'Description', 'Bruit anormal');
    selectionner(fixture, 'Criticité', 'High');

    await soumettre(fixture);

    controleur
      .expectOne((r) => r.method === 'POST')
      .flush(
        {
          title: 'Règle métier violée',
          status: 400,
          detail:
            "L'équipe est introuvable en base. Vérifiez que les données de référence sont à jour.",
        },
        { status: 400, statusText: 'Bad Request' },
      );
    await fixture.whenStable();

    expect(element(fixture).textContent).toContain(
      'La configuration des équipes ne couvre pas ce type',
    );
  });
});
