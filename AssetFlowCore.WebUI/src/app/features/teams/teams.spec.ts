import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EntraAuthService } from '../../core/auth/entra-auth.service';
import { errorInterceptor } from '../../core/http/error.interceptor';
import { TeamResponse } from '../../shared/models/team.model';
import { Teams } from './teams';

const EQUIPE_SERVEURS_HIGH: TeamResponse = {
  id: '11111111-1111-1111-1111-111111111111',
  name: 'Équipe Serveurs Critiques',
  description: null,
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
  assetType: 'Server',
  ticketCriticality: 'High',
};

const EQUIPE_PORTABLES_MEDIUM_INACTIVE: TeamResponse = {
  id: '22222222-2222-2222-2222-222222222222',
  name: 'Équipe Portables Standard',
  description: 'Équipe de secours',
  isActive: false,
  createdAt: '2026-01-02T00:00:00Z',
  assetType: 'Laptop',
  ticketCriticality: 'Medium',
};

/** Une équipe active par combinaison (type × criticité), pour couvrir les 9 combos du référentiel. */
function equipesCouvrantToutesLesCombinaisons(): readonly TeamResponse[] {
  return (['Server', 'Laptop', 'NetworkDevice'] as const).flatMap((assetType, indexType) =>
    (['Low', 'Medium', 'High'] as const).map((ticketCriticality, indexCriticite) => ({
      id: `33333333-3333-3333-3333-${indexType}${indexCriticite}00000000`,
      name: `Équipe ${assetType} ${ticketCriticality}`,
      description: null,
      isActive: true,
      createdAt: '2026-01-01T00:00:00Z',
      assetType,
      ticketCriticality,
    })),
  );
}

describe('Teams', () => {
  let controleur: HttpTestingController;

  // jsdom ne calcule aucune géométrie : sans ce stub, le piège de focus du CDK (`app-modal` /
  // `app-confirm-dialog` → `cdkTrapFocus`) considère qu'aucun élément n'est focusable.
  const geometrieInitiale = Element.prototype.getClientRects;

  beforeEach(() => {
    Element.prototype.getClientRects = function (): DOMRectList {
      return [{}] as unknown as DOMRectList;
    };

    TestBed.configureTestingModule({
      imports: [Teams],
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        // `JwtRolesService` → `EntraAuthService` → `Router`, nécessaire même sans navigation.
        provideRouter([]),
      ],
    });

    controleur = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    Element.prototype.getClientRects = geometrieInitiale;
    controleur.verify();
  });

  function element(fixture: ComponentFixture<Teams>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function texte(fixture: ComponentFixture<Teams>): string {
    return element(fixture).textContent ?? '';
  }

  function creer(): ComponentFixture<Teams> {
    const fixture = TestBed.createComponent(Teams);
    TestBed.tick();
    return fixture;
  }

  async function creerAvecEquipes(
    fixture: ComponentFixture<Teams>,
    equipes: readonly TeamResponse[] = [EQUIPE_SERVEURS_HIGH, EQUIPE_PORTABLES_MEDIUM_INACTIVE],
  ): Promise<void> {
    controleur.expectOne((r) => r.url === '/api/v1/teams').flush(equipes);
    await fixture.whenStable();
  }

  /** Recharge déclenchée par une action réussie : `isLoading()` repasse par `true` (voir
   * teams.ts, `recharger()`), donc tout le contenu de l'état « contenu » disparaît un instant
   * avant que le nouveau `GET` ne soit servi — même mécanique que `tickets/fiche.ts`. */
  async function repondreAuRechargement(
    fixture: ComponentFixture<Teams>,
    equipes: readonly TeamResponse[],
  ): Promise<void> {
    TestBed.tick();
    controleur.expectOne((r) => r.url === '/api/v1/teams').flush(equipes);
    await fixture.whenStable();
  }

  function boutonParLibelle(
    fixture: ComponentFixture<Teams>,
    libelle: string,
  ): HTMLButtonElement | null {
    return (
      [...element(fixture).querySelectorAll<HTMLButtonElement>('button')].find(
        (bouton) => bouton.textContent?.trim() === libelle,
      ) ?? null
    );
  }

  function existeBoutonLibelle(fixture: ComponentFixture<Teams>, libelle: string): boolean {
    return boutonParLibelle(fixture, libelle) !== null;
  }

  /** Les deux vues de `app-data-table` (table + cartes) coexistent dans le DOM : on cible le
   * bouton par son libellé **et** par la ligne (`tr`/`li`) qui contient le nom de l'équipe. */
  function boutonDansLigne(
    fixture: ComponentFixture<Teams>,
    nomEquipe: string,
    libelle: string,
  ): HTMLButtonElement {
    const trouve = [...element(fixture).querySelectorAll<HTMLButtonElement>('button')]
      .filter((bouton) => bouton.textContent?.trim() === libelle)
      .find((bouton) => bouton.closest('tr, li')?.textContent?.includes(nomEquipe) ?? false);
    if (trouve === undefined) {
      throw new Error(`Bouton "${libelle}" introuvable pour l'équipe "${nomEquipe}".`);
    }
    return trouve;
  }

  function ligneCouverture(fixture: ComponentFixture<Teams>, texteCombo: string): HTMLElement {
    const trouve = [...element(fixture).querySelectorAll<HTMLElement>('.grid > div')].find(
      (ligne) => (ligne.textContent ?? '').replace(/\s+/g, ' ').includes(texteCombo),
    );
    if (trouve === undefined) {
      throw new Error(`Ligne de couverture "${texteCombo}" introuvable.`);
    }
    return trouve;
  }

  function champ(fixture: ComponentFixture<Teams>, label: string): HTMLElement {
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

  function saisir(fixture: ComponentFixture<Teams>, label: string, valeur: string): void {
    const input = champ(fixture, label) as HTMLInputElement | HTMLTextAreaElement;
    input.value = valeur;
    input.dispatchEvent(new Event('input', { bubbles: true }));
  }

  function selectionner(fixture: ComponentFixture<Teams>, label: string, valeur: string): void {
    const select = champ(fixture, label) as HTMLSelectElement;
    select.value = valeur;
    select.dispatchEvent(new Event('change'));
  }

  async function ouvrirParLibelle(
    fixture: ComponentFixture<Teams>,
    libelle: string,
  ): Promise<void> {
    boutonParLibelle(fixture, libelle)?.click();
    await fixture.whenStable();
    await fixture.whenStable();
  }

  async function ouvrirDialogueLigne(
    fixture: ComponentFixture<Teams>,
    nomEquipe: string,
    libelle: string,
  ): Promise<void> {
    boutonDansLigne(fixture, nomEquipe, libelle).click();
    await fixture.whenStable();
    await fixture.whenStable();
  }

  function boutonDansDialogue(
    fixture: ComponentFixture<Teams>,
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

  // --- États ------------------------------------------------------------------------------------

  it('interroge la liste complète des équipes (équipes désactivées incluses)', () => {
    const fixture = creer();

    expect(texte(fixture)).toContain('Chargement');
    controleur.expectOne((r) => r.url === '/api/v1/teams').flush([EQUIPE_SERVEURS_HIGH]);
  });

  it('affiche une erreur avec une action de rechargement', async () => {
    const fixture = creer();

    controleur
      .expectOne((r) => r.url === '/api/v1/teams')
      .flush(
        { title: 'Erreur serveur', status: 500 },
        { status: 500, statusText: 'Internal Server Error' },
      );
    await fixture.whenStable();

    expect(element(fixture).querySelector('[role="alert"]')).not.toBeNull();
    expect(boutonParLibelle(fixture, 'Réessayer')).not.toBeNull();
  });

  it("affiche l'état vide avec une invitation à créer la première équipe", async () => {
    const fixture = creer();
    await creerAvecEquipes(fixture, []);

    expect(texte(fixture)).toContain('Aucune équipe enregistrée');
    expect(boutonParLibelle(fixture, 'Créer une équipe')).not.toBeNull();
  });

  it('affiche la liste des équipes, actives et désactivées', async () => {
    const fixture = creer();
    await creerAvecEquipes(fixture);

    const rendu = texte(fixture);
    expect(rendu).toContain('Équipe Serveurs Critiques');
    expect(rendu).toContain('Équipe Portables Standard');
  });

  // --- Couverture des 9 combinaisons (5.C.3, RM-12, RM-31) ---------------------------------------

  describe('Couverture des combinaisons type × criticité', () => {
    it('ignore une équipe désactivée : sa combinaison reste signalée comme non couverte', async () => {
      const fixture = creer();
      await creerAvecEquipes(fixture);

      expect(ligneCouverture(fixture, 'Ordinateur portable · Moyenne').textContent).toContain(
        'Non couverte',
      );
      expect(ligneCouverture(fixture, 'Serveur · Haute').textContent).toContain('Couverte');
      expect(texte(fixture)).toContain(
        'combinaisons type × criticité ne sont couvertes par aucune équipe active',
      );
    });

    it("n'affiche aucune alerte quand toutes les combinaisons sont couvertes", async () => {
      const fixture = creer();
      await creerAvecEquipes(fixture, equipesCouvrantToutesLesCombinaisons());

      expect(texte(fixture)).not.toContain('ne sont couvertes par aucune équipe active');
      expect(texte(fixture)).not.toContain("n'est couverte par aucune équipe active");
    });

    it('utilise le singulier quand une seule combinaison reste non couverte', async () => {
      const fixture = creer();
      const HUIT_EQUIPES = equipesCouvrantToutesLesCombinaisons().filter(
        (equipe) => !(equipe.assetType === 'Server' && equipe.ticketCriticality === 'High'),
      );
      await creerAvecEquipes(fixture, HUIT_EQUIPES);

      expect(texte(fixture)).toContain(
        "1 combinaison type × criticité n'est couverte par aucune équipe active",
      );
    });
  });

  // --- Rôle Administrateur — visibilité des actions de mutation ----------------------------------

  describe('Visibilité des actions selon le rôle', () => {
    it("propose créer/modifier/désactiver/supprimer tant qu'aucun tenant Entra ID n'est configuré (laissez-passer, étape 7.0)", async () => {
      const authentification = TestBed.inject(EntraAuthService);
      expect(authentification.estConfigure).toBe(false);

      const fixture = creer();
      await creerAvecEquipes(fixture, [EQUIPE_SERVEURS_HIGH]);

      expect(boutonParLibelle(fixture, 'Créer une équipe')).not.toBeNull();
      expect(existeBoutonLibelle(fixture, 'Modifier')).toBe(true);
      expect(existeBoutonLibelle(fixture, 'Désactiver')).toBe(true);
      expect(existeBoutonLibelle(fixture, 'Supprimer')).toBe(true);
    });

    it('masque ces actions sans le rôle Administrateur, une fois un tenant configuré', async () => {
      const authentification = TestBed.inject(EntraAuthService);
      Object.defineProperty(authentification, 'estConfigure', { value: true });

      const fixture = creer();
      await creerAvecEquipes(fixture, [EQUIPE_SERVEURS_HIGH]);

      expect(boutonParLibelle(fixture, 'Créer une équipe')).toBeNull();
      expect(existeBoutonLibelle(fixture, 'Modifier')).toBe(false);
      expect(existeBoutonLibelle(fixture, 'Désactiver')).toBe(false);
      expect(existeBoutonLibelle(fixture, 'Supprimer')).toBe(false);
    });
  });

  // --- Création (EF-23, RM-23, RM-24) -------------------------------------------------------------

  describe('Création', () => {
    it('valide localement les champs obligatoires, sans appel réseau', async () => {
      const fixture = creer();
      await creerAvecEquipes(fixture);

      await ouvrirParLibelle(fixture, 'Créer une équipe');
      boutonDansDialogue(fixture, 'Enregistrer').click();
      await fixture.whenStable();

      controleur.expectNone((r) => r.method === 'POST');
      expect(texte(fixture)).toContain('Ce champ est obligatoire');
    });

    it('crée une équipe avec succès, ferme la fenêtre et recharge la liste', async () => {
      const fixture = creer();
      await creerAvecEquipes(fixture);

      await ouvrirParLibelle(fixture, 'Créer une équipe');
      saisir(fixture, 'Nom', 'Équipe Réseau Secours');
      selectionner(fixture, "Type d'actif", 'NetworkDevice');
      selectionner(fixture, 'Criticité', 'Low');

      boutonDansDialogue(fixture, 'Enregistrer').click();

      const requete = controleur.expectOne((r) => r.method === 'POST' && r.url === '/api/v1/teams');
      expect(requete.request.body).toEqual({
        name: 'Équipe Réseau Secours',
        assetType: 'NetworkDevice',
        ticketCriticality: 'Low',
        description: null,
      });
      requete.flush(
        {
          id: '44444444-4444-4444-4444-444444444444',
          name: 'Équipe Réseau Secours',
          description: null,
          isActive: true,
          createdAt: '2026-08-08T00:00:00Z',
          assetType: 'NetworkDevice',
          ticketCriticality: 'Low',
        },
        { status: 201, statusText: 'Created' },
      );

      await repondreAuRechargement(fixture, [EQUIPE_SERVEURS_HIGH]);

      expect(element(fixture).querySelector('[role="dialog"]')).toBeNull();
    });

    it('signale un nom déjà pris comme erreur du champ « Nom », pas comme erreur globale (RM-23)', async () => {
      const fixture = creer();
      await creerAvecEquipes(fixture);

      await ouvrirParLibelle(fixture, 'Créer une équipe');
      saisir(fixture, 'Nom', 'Équipe Serveurs Critiques');
      selectionner(fixture, "Type d'actif", 'Server');
      selectionner(fixture, 'Criticité', 'Low');
      boutonDansDialogue(fixture, 'Enregistrer').click();

      controleur
        .expectOne((r) => r.method === 'POST')
        .flush(
          {
            title: 'Règle métier violée',
            status: 400,
            detail: "Une équipe nommée 'Équipe Serveurs Critiques' existe déjà.",
          },
          { status: 400, statusText: 'Bad Request' },
        );
      await fixture.whenStable();

      // Le message porte sur le champ, pas dans une bannière `app-error-message` autonome.
      expect(texte(fixture)).toContain('existe déjà');
      expect(element(fixture).querySelector('[role="dialog"]')).not.toBeNull();
    });
  });

  // --- Édition (EF-25, RM-27) ----------------------------------------------------------------------

  describe('Édition', () => {
    it("pré-remplit la fenêtre avec les valeurs actuelles de l'équipe", async () => {
      const fixture = creer();
      await creerAvecEquipes(fixture);

      await ouvrirDialogueLigne(fixture, EQUIPE_SERVEURS_HIGH.name, 'Modifier');

      const champNom = champ(fixture, 'Nom') as HTMLInputElement;
      const champType = champ(fixture, "Type d'actif") as HTMLSelectElement;
      expect(champNom.value).toBe('Équipe Serveurs Critiques');
      expect(champType.value).toBe('Server');
    });

    it('modifie une équipe avec succès', async () => {
      const fixture = creer();
      await creerAvecEquipes(fixture);

      await ouvrirDialogueLigne(fixture, EQUIPE_SERVEURS_HIGH.name, 'Modifier');
      saisir(fixture, 'Nom', 'Équipe Serveurs Critiques (Nord)');
      boutonDansDialogue(fixture, 'Enregistrer').click();

      const requete = controleur.expectOne(
        (r) => r.method === 'PUT' && r.url === `/api/v1/teams/${EQUIPE_SERVEURS_HIGH.id}`,
      );
      expect(requete.request.body).toEqual({
        name: 'Équipe Serveurs Critiques (Nord)',
        assetType: 'Server',
        ticketCriticality: 'High',
        description: null,
      });
      requete.flush({ ...EQUIPE_SERVEURS_HIGH, name: 'Équipe Serveurs Critiques (Nord)' });

      await repondreAuRechargement(fixture, [
        { ...EQUIPE_SERVEURS_HIGH, name: 'Équipe Serveurs Critiques (Nord)' },
      ]);

      expect(texte(fixture)).toContain('Équipe Serveurs Critiques (Nord)');
    });
  });

  // --- Suppression (EF-26, RM-25) ------------------------------------------------------------------

  describe('Suppression', () => {
    it('supprime une équipe avec succès et recharge la liste', async () => {
      const fixture = creer();
      await creerAvecEquipes(fixture);

      await ouvrirDialogueLigne(fixture, EQUIPE_SERVEURS_HIGH.name, 'Supprimer');
      boutonDansDialogue(fixture, 'Confirmer la suppression').click();

      controleur
        .expectOne(
          (r) => r.method === 'DELETE' && r.url === `/api/v1/teams/${EQUIPE_SERVEURS_HIGH.id}`,
        )
        .flush(null, { status: 204, statusText: 'No Content' });

      await repondreAuRechargement(fixture, [EQUIPE_PORTABLES_MEDIUM_INACTIVE]);

      expect(element(fixture).querySelector('[role="dialog"]')).toBeNull();
      expect(texte(fixture)).not.toContain('Équipe Serveurs Critiques');
    });

    it('refuse la suppression en laissant la boîte ouverte, quand des incidents référencent encore l’équipe (RM-25)', async () => {
      const fixture = creer();
      await creerAvecEquipes(fixture);

      await ouvrirDialogueLigne(fixture, EQUIPE_SERVEURS_HIGH.name, 'Supprimer');
      boutonDansDialogue(fixture, 'Confirmer la suppression').click();

      controleur
        .expectOne((r) => r.method === 'DELETE')
        .flush(
          {
            title: 'Règle métier violée',
            status: 400,
            detail: 'Impossible de supprimer le team : des tickets actifs lui sont assignes.',
          },
          { status: 400, statusText: 'Bad Request' },
        );
      await fixture.whenStable();

      expect(element(fixture).querySelector('[role="dialog"]')).not.toBeNull();
      expect(texte(fixture)).toContain('Impossible de supprimer le team');
    });
  });

  // --- Désactivation (décision 0.6, RM-30, RM-31) ---------------------------------------------------

  describe('Désactivation', () => {
    it('désactive une équipe avec succès sans avertissement particulier quand une autre équipe couvre encore la combinaison', async () => {
      const fixture = creer();
      const EQUIPE_SERVEURS_HIGH_BIS: TeamResponse = {
        ...EQUIPE_SERVEURS_HIGH,
        id: '55555555-5555-5555-5555-555555555555',
        name: 'Équipe Serveurs Critiques (Sud)',
      };
      await creerAvecEquipes(fixture, [EQUIPE_SERVEURS_HIGH, EQUIPE_SERVEURS_HIGH_BIS]);

      await ouvrirDialogueLigne(fixture, EQUIPE_SERVEURS_HIGH.name, 'Désactiver');

      expect(texte(fixture)).not.toContain('dernière équipe active de cette combinaison');

      boutonDansDialogue(fixture, 'Confirmer la désactivation').click();

      controleur
        .expectOne(
          (r) =>
            r.method === 'PUT' && r.url === `/api/v1/teams/${EQUIPE_SERVEURS_HIGH.id}/deactivate`,
        )
        .flush({ ...EQUIPE_SERVEURS_HIGH, isActive: false });

      await repondreAuRechargement(fixture, [
        { ...EQUIPE_SERVEURS_HIGH, isActive: false },
        EQUIPE_SERVEURS_HIGH_BIS,
      ]);

      expect(element(fixture).querySelector('[role="dialog"]')).toBeNull();
    });

    it("avertit avant confirmation quand c'est la dernière équipe active de sa combinaison (RM-31)", async () => {
      const fixture = creer();
      await creerAvecEquipes(fixture, [EQUIPE_SERVEURS_HIGH]);

      await ouvrirDialogueLigne(fixture, EQUIPE_SERVEURS_HIGH.name, 'Désactiver');

      expect(texte(fixture)).toContain('dernière équipe active de cette combinaison');
    });
  });

  // --- Activation (action directe, sans boîte de dialogue) -------------------------------------

  describe('Activation', () => {
    it('active une équipe désactivée directement et recharge la liste', async () => {
      const fixture = creer();
      await creerAvecEquipes(fixture);

      boutonDansLigne(fixture, EQUIPE_PORTABLES_MEDIUM_INACTIVE.name, 'Activer').click();

      controleur
        .expectOne(
          (r) =>
            r.method === 'PUT' &&
            r.url === `/api/v1/teams/${EQUIPE_PORTABLES_MEDIUM_INACTIVE.id}/activate`,
        )
        .flush({ ...EQUIPE_PORTABLES_MEDIUM_INACTIVE, isActive: true });

      await repondreAuRechargement(fixture, [
        EQUIPE_SERVEURS_HIGH,
        { ...EQUIPE_PORTABLES_MEDIUM_INACTIVE, isActive: true },
      ]);

      expect(ligneCouverture(fixture, 'Ordinateur portable · Moyenne').textContent).toContain(
        'Couverte',
      );
    });

    it("affiche le message d'erreur sans faire disparaître la liste quand l'activation échoue", async () => {
      const fixture = creer();
      await creerAvecEquipes(fixture);

      boutonDansLigne(fixture, EQUIPE_PORTABLES_MEDIUM_INACTIVE.name, 'Activer').click();

      controleur
        .expectOne(
          (r) =>
            r.method === 'PUT' &&
            r.url === `/api/v1/teams/${EQUIPE_PORTABLES_MEDIUM_INACTIVE.id}/activate`,
        )
        .flush(
          { title: 'Erreur serveur', status: 500 },
          { status: 500, statusText: 'Internal Server Error' },
        );
      await fixture.whenStable();

      expect(element(fixture).querySelector('[role="alert"]')).not.toBeNull();
      expect(texte(fixture)).toContain(EQUIPE_PORTABLES_MEDIUM_INACTIVE.name);
    });
  });
});
