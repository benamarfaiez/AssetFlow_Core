import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EntraAuthService } from '../../../core/auth/entra-auth.service';
import { errorInterceptor } from '../../../core/http/error.interceptor';
import { AssetDetailResponse, AssetTicketSummary } from '../../../shared/models/asset.model';
import { Fiche } from './fiche';

const ID = '11111111-1111-1111-1111-111111111111';

const TICKET: AssetTicketSummary = {
  id: '22222222-2222-2222-2222-222222222222',
  title: 'Ventilateur bruyant',
  criticality: 'High',
  status: 'Opened',
  createdAt: '2026-08-01T10:00:00Z',
  assignedTeamId: '33333333-3333-3333-3333-333333333333',
  assignedTeamName: 'Équipe Serveurs Critiques',
};

const ACTIF: AssetDetailResponse = {
  id: ID,
  name: 'Serveur de sauvegarde',
  serialNumber: 'SRV-00042',
  type: 'Server',
  status: 'InService',
  createdAt: '2026-08-05T09:00:00Z',
  tickets: [],
};

describe('Fiche', () => {
  let controleur: HttpTestingController;

  // jsdom ne calcule aucune géométrie : sans ce stub, le piège de focus du CDK (`ConfirmDialog` →
  // `Modal` → `cdkTrapFocus`) considère qu'aucun élément n'est focusable — même stub que
  // `confirm-dialog.spec.ts`/`modal.spec.ts`.
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
        // `JwtRolesService` → `EntraAuthService` → `Router` : nécessaire même quand un test ne
        // navigue pas, à l'image de `jwt-roles.service.spec.ts`.
        provideRouter([]),
      ],
    });

    controleur = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    Element.prototype.getClientRects = geometrieInitiale;
    controleur.verify();
  });

  /** Crée le composant avec son entrée `id` et laisse partir l'appel émis par la ressource. */
  function creer(id: string): ComponentFixture<Fiche> {
    const fixture = TestBed.createComponent(Fiche);
    fixture.componentRef.setInput('id', id);
    TestBed.tick();
    return fixture;
  }

  function element(fixture: ComponentFixture<Fiche>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function texte(fixture: ComponentFixture<Fiche>): string {
    return element(fixture).textContent ?? '';
  }

  /** Répond à l'appel initial de la ressource et laisse le rendu se stabiliser. */
  async function creerAvecActif(
    fixture: ComponentFixture<Fiche>,
    actif: AssetDetailResponse = ACTIF,
  ): Promise<void> {
    controleur.expectOne(`/api/v1/assets/${ID}`).flush(actif);
    await fixture.whenStable();
  }

  /** Bouton de la page elle-même (hors boîte de dialogue). */
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

  /** Bouton à l'intérieur de la boîte de dialogue actuellement ouverte. */
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

  /**
   * Clique sur le bouton déclencheur et attend le second rendu dû au piège de focus du CDK — même
   * exigence que `confirm-dialog.spec.ts` : le focus se déplace au rendu qui suit celui de
   * l'ouverture.
   */
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

  /** Simule la saisie d'un motif dans la zone de texte projetée de la boîte de remise en service. */
  async function saisirMotif(fixture: ComponentFixture<Fiche>, valeur: string): Promise<void> {
    const zoneTexte = element(fixture).querySelector<HTMLTextAreaElement>(
      '[role="dialog"] textarea',
    );
    if (zoneTexte === null) {
      throw new Error('La zone de texte du motif est introuvable.');
    }
    zoneTexte.value = valeur;
    zoneTexte.dispatchEvent(new Event('input', { bubbles: true }));
    await fixture.whenStable();
  }

  it('interroge la fiche correspondant à l’identifiant reçu', () => {
    const fixture = creer(ID);

    expect(texte(fixture)).toContain('Chargement');
    controleur.expectOne(`/api/v1/assets/${ID}`).flush(ACTIF);
  });

  it('affiche la fiche reçue', async () => {
    const fixture = creer(ID);
    await creerAvecActif(fixture);

    expect(texte(fixture)).toContain('Serveur de sauvegarde');
  });

  it("traduit un 404 sans exposer l'identifiant technique", async () => {
    const fixture = creer(ID);

    controleur
      .expectOne(`/api/v1/assets/${ID}`)
      .flush(
        { title: 'Ressource introuvable', status: 404, detail: `L'actif ${ID} est introuvable.` },
        { status: 404, statusText: 'Not Found' },
      );
    await fixture.whenStable();

    expect(element(fixture).querySelector('[role="alert"]')).not.toBeNull();
    expect(texte(fixture)).not.toContain(ID);
  });

  it('affiche le type, le statut, le numéro de série et les incidents de la fiche', async () => {
    const fixture = creer(ID);
    await creerAvecActif(fixture, { ...ACTIF, tickets: [TICKET] });

    const rendu = texte(fixture);
    expect(rendu).toContain('Serveur');
    expect(rendu).toContain('SRV-00042');
    expect(rendu).toContain('En service');
    expect(rendu).toContain('Ventilateur bruyant');
    expect(rendu).toContain('Haute');
    expect(rendu).toContain('Ouvert');
  });

  describe('Lien « Ouvrir un incident » (P-02, RM-09)', () => {
    it('pointe vers le formulaire d’ouverture avec l’actif pré-rempli, pour un actif en service', async () => {
      const fixture = creer(ID);
      await creerAvecActif(fixture);

      const lien = element(fixture).querySelector<HTMLAnchorElement>(
        `a[href="/tickets/nouveau?assetId=${ID}"]`,
      );
      expect(lien?.textContent?.trim()).toBe('Ouvrir un incident');
    });

    it('est absent pour un actif déjà mis au rebut (RM-09)', async () => {
      const fixture = creer(ID);
      await creerAvecActif(fixture, { ...ACTIF, status: 'Decommissioned' });

      expect(element(fixture).querySelector(`a[href^="/tickets/nouveau"]`)).toBeNull();
    });
  });

  it("n'affiche pas d'action « Mettre au rebut » pour un actif déjà mis au rebut", async () => {
    const fixture = creer(ID);
    await creerAvecActif(fixture, { ...ACTIF, status: 'Decommissioned' });

    expect(boutonParLibelle(fixture, 'Mettre au rebut')).toBeNull();
  });

  describe('« Remettre en service » — visibilité', () => {
    it("n'est jamais proposée pour un actif en service", async () => {
      const fixture = creer(ID);
      await creerAvecActif(fixture);

      expect(boutonParLibelle(fixture, 'Remettre en service')).toBeNull();
    });

    it('est proposée pour un actif au rebut tant qu’aucun tenant Entra ID n’est configuré (laissez-passer, étape 7.0)', async () => {
      const authentification = TestBed.inject(EntraAuthService);
      expect(authentification.estConfigure).toBe(false);

      const fixture = creer(ID);
      await creerAvecActif(fixture, { ...ACTIF, status: 'Decommissioned' });

      expect(boutonParLibelle(fixture, 'Remettre en service')).not.toBeNull();
    });

    it('est masquée pour un actif au rebut sans le rôle Administrateur, une fois un tenant configuré', async () => {
      const authentification = TestBed.inject(EntraAuthService);
      // `estConfigure` est calculé à la construction : forcé ici comme dans `auth.guard.spec.ts`
      // pour exercer la branche « rôles réels » sans jeton (donc sans le rôle Administrateur).
      Object.defineProperty(authentification, 'estConfigure', { value: true });

      const fixture = creer(ID);
      await creerAvecActif(fixture, { ...ACTIF, status: 'Decommissioned' });

      expect(boutonParLibelle(fixture, 'Remettre en service')).toBeNull();
    });
  });

  describe('Mise au rebut (P-06)', () => {
    it('refuse la mise au rebut en indiquant le nombre exact d’incidents actifs, sans fermer la boîte de dialogue', async () => {
      const fixture = creer(ID);
      await creerAvecActif(fixture);

      await ouvrirDialogue(fixture, 'Mettre au rebut');
      boutonDansDialogue(fixture, 'Confirmer la mise au rebut').click();

      controleur.expectOne(`/api/v1/assets/${ID}/decommission`).flush(
        {
          title: 'Requête invalide',
          status: 400,
          detail:
            "Action interdite : l'actif fait l'objet de 2 incident(s) en cours de traitement.",
        },
        { status: 400, statusText: 'Bad Request' },
      );
      await fixture.whenStable();

      expect(texte(fixture)).toContain('a 2 incident(s) en cours');
      expect(element(fixture).querySelector('[role="dialog"]')).not.toBeNull();
    });

    it("affiche un message dédié sur 404, sans exposer l'identifiant technique", async () => {
      const fixture = creer(ID);
      await creerAvecActif(fixture);

      await ouvrirDialogue(fixture, 'Mettre au rebut');
      boutonDansDialogue(fixture, 'Confirmer la mise au rebut').click();

      controleur
        .expectOne(`/api/v1/assets/${ID}/decommission`)
        .flush(
          { title: 'Ressource introuvable', status: 404, detail: `L'actif ${ID} est introuvable.` },
          { status: 404, statusText: 'Not Found' },
        );
      await fixture.whenStable();

      expect(texte(fixture)).toContain("Cet équipement n'existe plus");
      expect(texte(fixture)).not.toContain(ID);
    });

    it('met au rebut avec succès, recharge la fiche puis ferme la boîte de dialogue', async () => {
      const fixture = creer(ID);
      await creerAvecActif(fixture);

      await ouvrirDialogue(fixture, 'Mettre au rebut');
      boutonDansDialogue(fixture, 'Confirmer la mise au rebut').click();

      controleur
        .expectOne(`/api/v1/assets/${ID}/decommission`)
        .flush(null, { status: 204, statusText: 'No Content' });
      TestBed.tick();

      controleur.expectOne(`/api/v1/assets/${ID}`).flush({ ...ACTIF, status: 'Decommissioned' });
      await fixture.whenStable();

      expect(element(fixture).querySelector('[role="dialog"]')).toBeNull();
      expect(texte(fixture)).toContain('Mis au rebut');
    });
  });

  describe('Remise en service (P-06 bis)', () => {
    /** Un actif déjà mis au rebut ; aucun tenant configuré en test ⇒ rôle Administrateur acquis. */
    async function creerActifDecommissionne(fixture: ComponentFixture<Fiche>): Promise<void> {
      await creerAvecActif(fixture, { ...ACTIF, status: 'Decommissioned' });
    }

    it("valide localement le motif vide et n'appelle pas l'API", async () => {
      const fixture = creer(ID);
      await creerActifDecommissionne(fixture);

      await ouvrirDialogue(fixture, 'Remettre en service');
      boutonDansDialogue(fixture, 'Confirmer la remise en service').click();
      await fixture.whenStable();

      controleur.expectNone(`/api/v1/assets/${ID}/restore-to-service`);
      expect(texte(fixture)).toContain('Le motif de remise en service est obligatoire.');
      expect(element(fixture).querySelector('[role="dialog"]')).not.toBeNull();
    });

    it("valide localement un motif composé uniquement d'espaces et n'appelle pas l'API", async () => {
      const fixture = creer(ID);
      await creerActifDecommissionne(fixture);

      await ouvrirDialogue(fixture, 'Remettre en service');
      await saisirMotif(fixture, '   ');
      boutonDansDialogue(fixture, 'Confirmer la remise en service').click();
      await fixture.whenStable();

      controleur.expectNone(`/api/v1/assets/${ID}/restore-to-service`);
      expect(texte(fixture)).toContain('Le motif de remise en service est obligatoire.');
    });

    it('remet en service avec succès, recharge la fiche puis ferme la boîte de dialogue', async () => {
      const fixture = creer(ID);
      await creerActifDecommissionne(fixture);

      await ouvrirDialogue(fixture, 'Remettre en service');
      await saisirMotif(fixture, 'Rebut par erreur');
      boutonDansDialogue(fixture, 'Confirmer la remise en service').click();

      const requete = controleur.expectOne(`/api/v1/assets/${ID}/restore-to-service`);
      expect(requete.request.body).toEqual({ reason: 'Rebut par erreur' });
      requete.flush(null, { status: 204, statusText: 'No Content' });
      TestBed.tick();

      controleur.expectOne(`/api/v1/assets/${ID}`).flush(ACTIF);
      await fixture.whenStable();

      expect(element(fixture).querySelector('[role="dialog"]')).toBeNull();
      expect(texte(fixture)).toContain('En service');
    });

    it('affiche le refus serveur (403) lisiblement, sans casser l’écran', async () => {
      const fixture = creer(ID);
      await creerActifDecommissionne(fixture);

      await ouvrirDialogue(fixture, 'Remettre en service');
      await saisirMotif(fixture, 'Rebut par erreur');
      boutonDansDialogue(fixture, 'Confirmer la remise en service').click();

      controleur
        .expectOne(`/api/v1/assets/${ID}/restore-to-service`)
        .flush({ title: 'Interdit', status: 403 }, { status: 403, statusText: 'Forbidden' });
      await fixture.whenStable();

      expect(texte(fixture)).toContain("Vous n'avez pas les droits nécessaires");
      expect(element(fixture).querySelector('[role="dialog"]')).not.toBeNull();
    });
  });
});
