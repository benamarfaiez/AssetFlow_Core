import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { errorInterceptor } from '../../../core/http/error.interceptor';
import { TeamResponse } from '../../../shared/models/team.model';
import { TicketResponse } from '../../../shared/models/ticket.model';
import { Fiche } from './fiche';

const ID = '11111111-1111-1111-1111-111111111111';

const INCIDENT_OUVERT: TicketResponse = {
  id: ID,
  assetId: '22222222-2222-2222-2222-222222222222',
  title: 'Ventilateur bruyant',
  description: 'Bruit anormal détecté au démarrage',
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

const EQUIPE_CIBLE: TeamResponse = {
  id: '55555555-5555-5555-5555-555555555555',
  name: 'Équipe Serveurs Standard',
  description: null,
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
  assetType: 'Server',
  ticketCriticality: 'Medium',
};

describe('Fiche (tickets)', () => {
  let controleur: HttpTestingController;
  const geometrieInitiale = Element.prototype.getClientRects;

  beforeEach(() => {
    Element.prototype.getClientRects = function (): DOMRectList {
      return [{}] as unknown as DOMRectList;
    };

    TestBed.configureTestingModule({
      imports: [Fiche],
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });

    controleur = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    Element.prototype.getClientRects = geometrieInitiale;
    controleur.verify();
  });

  function element(fixture: ComponentFixture<Fiche>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function texte(fixture: ComponentFixture<Fiche>): string {
    return element(fixture).textContent ?? '';
  }

  function creer(id: string): ComponentFixture<Fiche> {
    const fixture = TestBed.createComponent(Fiche);
    fixture.componentRef.setInput('id', id);
    TestBed.tick();
    return fixture;
  }

  /** Répond aux deux appels initiaux (incident + équipes actives) et laisse le rendu se stabiliser. */
  async function creerAvecIncident(
    fixture: ComponentFixture<Fiche>,
    incident: TicketResponse = INCIDENT_OUVERT,
    equipes: readonly TeamResponse[] = [EQUIPE_CIBLE],
  ): Promise<void> {
    controleur.expectOne(`/api/v1/tickets/${ID}`).flush(incident);
    controleur.expectOne((r) => r.url === '/api/v1/teams').flush(equipes);
    await fixture.whenStable();
  }

  function boutonParLibelle(
    fixture: ComponentFixture<Fiche>,
    libelle: string,
  ): HTMLButtonElement | null {
    return (
      [...element(fixture).querySelectorAll<HTMLButtonElement>('button')].find(
        (bouton) => bouton.textContent?.trim() === libelle,
      ) ?? null
    );
  }

  function boutonDansDialogue(
    fixture: ComponentFixture<Fiche>,
    libelle: string,
  ): HTMLButtonElement {
    const trouve = [
      ...element(fixture).querySelectorAll<HTMLButtonElement>('[role="dialog"] button'),
    ].find((bouton) => bouton.textContent?.trim() === libelle);
    if (trouve === undefined) {
      throw new Error(`Bouton "${libelle}" introuvable dans la boîte de dialogue.`);
    }
    return trouve;
  }

  async function ouvrirDialogue(
    fixture: ComponentFixture<Fiche>,
    libelleDeclencheur: string,
  ): Promise<void> {
    const declencheur = boutonParLibelle(fixture, libelleDeclencheur);
    if (declencheur === null) {
      throw new Error(`Bouton déclencheur "${libelleDeclencheur}" introuvable.`);
    }
    declencheur.click();
    await fixture.whenStable();
    await fixture.whenStable();
  }

  function saisirDansDialogue(
    fixture: ComponentFixture<Fiche>,
    selecteur: string,
    valeur: string,
  ): void {
    const champ = element(fixture).querySelector<HTMLTextAreaElement>(
      `[role="dialog"] ${selecteur}`,
    );
    if (champ === null) {
      throw new Error(`Champ "${selecteur}" introuvable dans la boîte de dialogue.`);
    }
    champ.value = valeur;
    champ.dispatchEvent(new Event('input', { bubbles: true }));
  }

  it('interroge l’incident correspondant à l’identifiant reçu', () => {
    const fixture = creer(ID);

    expect(texte(fixture)).toContain('Chargement');
    controleur.expectOne(`/api/v1/tickets/${ID}`).flush(INCIDENT_OUVERT);
    controleur.expectOne((r) => r.url === '/api/v1/teams').flush([EQUIPE_CIBLE]);
  });

  it('affiche le titre, la description, l’équipe et l’état « analyse IA en cours »', async () => {
    const fixture = creer(ID);
    await creerAvecIncident(fixture);

    const rendu = texte(fixture);
    expect(rendu).toContain('Ventilateur bruyant');
    expect(rendu).toContain('Bruit anormal détecté au démarrage');
    expect(rendu).toContain('Équipe Serveurs Critiques');
    expect(rendu).toContain('Analyse IA en cours');
  });

  it("traduit un 404 sans exposer l'identifiant technique", async () => {
    const fixture = creer(ID);

    controleur
      .expectOne(`/api/v1/tickets/${ID}`)
      .flush(
        { title: 'Ressource introuvable', status: 404, detail: `Le ticket ${ID} est introuvable.` },
        { status: 404, statusText: 'Not Found' },
      );
    controleur.expectOne((r) => r.url === '/api/v1/teams').flush([]);
    await fixture.whenStable();

    expect(element(fixture).querySelector('[role="alert"]')).not.toBeNull();
    expect(texte(fixture)).not.toContain(ID);
  });

  describe('Visibilité des actions selon l’état', () => {
    it('propose « Prendre en charge » et « Transférer » sur un incident ouvert, jamais « Clôturer »', async () => {
      const fixture = creer(ID);
      await creerAvecIncident(fixture);

      expect(boutonParLibelle(fixture, 'Prendre en charge')).not.toBeNull();
      expect(boutonParLibelle(fixture, "Transférer l'incident")).not.toBeNull();
      expect(boutonParLibelle(fixture, "Clôturer l'incident")).toBeNull();
    });

    it('propose « Clôturer » et « Transférer » sur un incident en cours, jamais « Prendre en charge »', async () => {
      const fixture = creer(ID);
      await creerAvecIncident(fixture, { ...INCIDENT_OUVERT, status: 'InProgress' });

      expect(boutonParLibelle(fixture, 'Prendre en charge')).toBeNull();
      expect(boutonParLibelle(fixture, "Clôturer l'incident")).not.toBeNull();
      expect(boutonParLibelle(fixture, "Transférer l'incident")).not.toBeNull();
    });

    it("n'offre aucune action sur un incident clôturé", async () => {
      const fixture = creer(ID);
      await creerAvecIncident(fixture, {
        ...INCIDENT_OUVERT,
        status: 'Closed',
        resolutionComment: 'Ventilateur remplacé.',
      });

      expect(boutonParLibelle(fixture, 'Prendre en charge')).toBeNull();
      expect(boutonParLibelle(fixture, "Clôturer l'incident")).toBeNull();
      expect(boutonParLibelle(fixture, "Transférer l'incident")).toBeNull();
      expect(texte(fixture)).toContain('Ventilateur remplacé.');
    });
  });

  describe('Prendre en charge (P-03)', () => {
    it('prend en charge avec succès puis recharge la fiche', async () => {
      const fixture = creer(ID);
      await creerAvecIncident(fixture);

      boutonParLibelle(fixture, 'Prendre en charge')?.click();
      TestBed.tick();

      controleur
        .expectOne((r) => r.method === 'PUT' && r.url === `/api/v1/tickets/${ID}/assign`)
        .flush(null, { status: 204, statusText: 'No Content' });
      TestBed.tick();

      controleur
        .expectOne(`/api/v1/tickets/${ID}`)
        .flush({ ...INCIDENT_OUVERT, status: 'InProgress' });
      await fixture.whenStable();

      expect(boutonParLibelle(fixture, "Clôturer l'incident")).not.toBeNull();
    });

    it('affiche le message de conflit avec une action de rechargement sur un 409', async () => {
      const fixture = creer(ID);
      await creerAvecIncident(fixture);

      boutonParLibelle(fixture, 'Prendre en charge')?.click();
      TestBed.tick();

      controleur
        .expectOne((r) => r.method === 'PUT' && r.url === `/api/v1/tickets/${ID}/assign`)
        .flush(
          { title: 'Concurrence d’accès détectée', status: 409 },
          { status: 409, statusText: 'Conflict' },
        );
      await fixture.whenStable();

      expect(texte(fixture)).toContain("modifié par quelqu'un d'autre");
      expect(boutonParLibelle(fixture, 'Recharger')).not.toBeNull();
    });
  });

  describe('Clôturer (P-04, RM-16, RM-22)', () => {
    it("valide localement un compte rendu vide et n'appelle pas l'API", async () => {
      const fixture = creer(ID);
      await creerAvecIncident(fixture, { ...INCIDENT_OUVERT, status: 'InProgress' });

      await ouvrirDialogue(fixture, "Clôturer l'incident");
      boutonDansDialogue(fixture, 'Confirmer la clôture').click();
      await fixture.whenStable();

      controleur.expectNone((r) => r.url === `/api/v1/tickets/${ID}/close`);
      expect(texte(fixture)).toContain('Le compte rendu de résolution est obligatoire.');
    });

    it('clôture avec succès, recharge la fiche puis ferme la boîte de dialogue', async () => {
      const fixture = creer(ID);
      await creerAvecIncident(fixture, { ...INCIDENT_OUVERT, status: 'InProgress' });

      await ouvrirDialogue(fixture, "Clôturer l'incident");
      saisirDansDialogue(fixture, 'textarea', 'Ventilateur remplacé.');
      boutonDansDialogue(fixture, 'Confirmer la clôture').click();

      const requete = controleur.expectOne(
        (r) => r.method === 'PUT' && r.url === `/api/v1/tickets/${ID}/close`,
      );
      expect(requete.request.body).toEqual({ resolutionComment: 'Ventilateur remplacé.' });
      requete.flush(null, { status: 204, statusText: 'No Content' });
      TestBed.tick();

      controleur.expectOne(`/api/v1/tickets/${ID}`).flush({
        ...INCIDENT_OUVERT,
        status: 'Closed',
        resolutionComment: 'Ventilateur remplacé.',
      });
      await fixture.whenStable();

      expect(element(fixture).querySelector('[role="dialog"]')).toBeNull();
      expect(texte(fixture)).toContain('Ventilateur remplacé.');
    });

    it('conserve le compte rendu saisi et laisse la boîte ouverte sur un conflit 409 (RM-22)', async () => {
      const fixture = creer(ID);
      await creerAvecIncident(fixture, { ...INCIDENT_OUVERT, status: 'InProgress' });

      await ouvrirDialogue(fixture, "Clôturer l'incident");
      saisirDansDialogue(fixture, 'textarea', 'Ventilateur remplacé.');
      boutonDansDialogue(fixture, 'Confirmer la clôture').click();

      controleur
        .expectOne((r) => r.method === 'PUT' && r.url === `/api/v1/tickets/${ID}/close`)
        .flush(
          { title: 'Concurrence d’accès détectée', status: 409 },
          { status: 409, statusText: 'Conflict' },
        );
      await fixture.whenStable();

      // La boîte reste ouverte, le message de conflit s'affiche, et surtout la saisie n'est pas
      // perdue : c'est exactement ce que RM-22 exige.
      expect(element(fixture).querySelector('[role="dialog"]')).not.toBeNull();
      expect(texte(fixture)).toContain("modifié par quelqu'un d'autre");
      const zoneTexte = element(fixture).querySelector<HTMLTextAreaElement>(
        '[role="dialog"] textarea',
      );
      expect(zoneTexte?.value).toBe('Ventilateur remplacé.');
    });
  });

  describe('Transférer (P-05, RM-19, RM-22)', () => {
    it("valide localement l'absence d'équipe et de motif, et n'appelle pas l'API", async () => {
      const fixture = creer(ID);
      await creerAvecIncident(fixture);

      await ouvrirDialogue(fixture, "Transférer l'incident");
      boutonDansDialogue(fixture, 'Confirmer le transfert').click();
      await fixture.whenStable();

      controleur.expectNone((r) => r.url === `/api/v1/tickets/${ID}/transfer`);
      expect(texte(fixture)).toContain('Le motif du transfert est obligatoire.');
    });

    it('exclut l’équipe déjà assignée du sélecteur de destination', async () => {
      const fixture = creer(ID);
      await creerAvecIncident(fixture, INCIDENT_OUVERT, [
        EQUIPE_CIBLE,
        {
          ...EQUIPE_CIBLE,
          id: '66666666-6666-6666-6666-666666666666',
          name: 'Équipe Serveurs Critiques',
        },
      ]);

      await ouvrirDialogue(fixture, "Transférer l'incident");

      const options = [
        ...element(fixture).querySelectorAll<HTMLOptionElement>('[role="dialog"] option'),
      ].map((option) => option.textContent?.trim());
      expect(options).toContain('Équipe Serveurs Standard');
      expect(options).not.toContain('Équipe Serveurs Critiques');
    });

    it('transfère avec succès en envoyant le nom de l’équipe cible, puis recharge la fiche', async () => {
      const fixture = creer(ID);
      await creerAvecIncident(fixture);

      await ouvrirDialogue(fixture, "Transférer l'incident");

      const select = element(fixture).querySelector<HTMLSelectElement>('[role="dialog"] select');
      if (select === null) {
        throw new Error('Sélecteur d’équipe introuvable.');
      }
      select.value = EQUIPE_CIBLE.name;
      select.dispatchEvent(new Event('change'));
      saisirDansDialogue(fixture, 'textarea', 'Mauvais routage initial.');

      boutonDansDialogue(fixture, 'Confirmer le transfert').click();

      const requete = controleur.expectOne(
        (r) => r.method === 'POST' && r.url === `/api/v1/tickets/${ID}/transfer`,
      );
      expect(requete.request.body).toEqual({
        targetTeam: EQUIPE_CIBLE.name,
        reason: 'Mauvais routage initial.',
      });
      requete.flush(null, { status: 204, statusText: 'No Content' });
      TestBed.tick();

      controleur
        .expectOne(`/api/v1/tickets/${ID}`)
        .flush({ ...INCIDENT_OUVERT, assignedTeamName: EQUIPE_CIBLE.name });
      await fixture.whenStable();

      expect(element(fixture).querySelector('[role="dialog"]')).toBeNull();
      expect(texte(fixture)).toContain(EQUIPE_CIBLE.name);
    });

    it('conserve la sélection et le motif, et laisse la boîte ouverte sur un conflit 409 (RM-22)', async () => {
      const fixture = creer(ID);
      await creerAvecIncident(fixture);

      await ouvrirDialogue(fixture, "Transférer l'incident");

      const select = element(fixture).querySelector<HTMLSelectElement>('[role="dialog"] select');
      if (select === null) {
        throw new Error('Sélecteur d’équipe introuvable.');
      }
      select.value = EQUIPE_CIBLE.name;
      select.dispatchEvent(new Event('change'));
      saisirDansDialogue(fixture, 'textarea', 'Mauvais routage initial.');

      boutonDansDialogue(fixture, 'Confirmer le transfert').click();

      controleur
        .expectOne((r) => r.method === 'POST' && r.url === `/api/v1/tickets/${ID}/transfer`)
        .flush(
          { title: 'Concurrence d’accès détectée', status: 409 },
          { status: 409, statusText: 'Conflict' },
        );
      await fixture.whenStable();

      // Même exigence RM-22 que pour la clôture : la boîte reste ouverte et la saisie est intacte.
      expect(element(fixture).querySelector('[role="dialog"]')).not.toBeNull();
      expect(texte(fixture)).toContain("modifié par quelqu'un d'autre");
      const zoneTexte = element(fixture).querySelector<HTMLTextAreaElement>(
        '[role="dialog"] textarea',
      );
      expect(zoneTexte?.value).toBe('Mauvais routage initial.');
      expect(select.value).toBe(EQUIPE_CIBLE.name);
    });
  });
});
