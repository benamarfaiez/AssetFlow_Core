import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { errorInterceptor } from '../../../core/http/error.interceptor';
import { AssetResponse } from '../../../shared/models/asset.model';
import { Inventaire } from './inventaire';
import { InventaireService } from './inventaire.service';

const SERVEUR: AssetResponse = {
  id: '11111111-1111-1111-1111-111111111111',
  name: 'Serveur de sauvegarde',
  serialNumber: 'SRV-00042',
  type: 'Server',
  status: 'InService',
  createdAt: '2026-08-05T09:00:00Z',
};

const PORTABLE_EN_PANNE: AssetResponse = {
  id: '22222222-2222-2222-2222-222222222222',
  name: 'Portable support N3',
  serialNumber: 'LAP-00017',
  type: 'Laptop',
  status: 'Down',
  createdAt: '2026-08-04T09:00:00Z',
};

describe('Inventaire', () => {
  let controleur: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Inventaire],
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        // `InventaireService` n'est plus `providedIn: 'root'` : en production, il vient du
        // provider de la route parente `assets` (voir `assets.routes.ts`), absente ici puisque
        // le composant est monté directement. Sans cette ligne, l'injection échouerait.
        InventaireService,
      ],
    });

    controleur = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controleur.verify());

  /** Crée le composant et laisse partir l'appel émis par la ressource. */
  function creer(): ComponentFixture<Inventaire> {
    const fixture = TestBed.createComponent(Inventaire);
    TestBed.tick();
    return fixture;
  }

  function element(fixture: ComponentFixture<Inventaire>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function texte(fixture: ComponentFixture<Inventaire>): string {
    return element(fixture).textContent ?? '';
  }

  /** Répond à l'appel initial et laisse le rendu se stabiliser. */
  async function creerAvecActifs(
    fixture: ComponentFixture<Inventaire>,
    actifs: readonly AssetResponse[],
  ): Promise<void> {
    controleur.expectOne('/api/v1/assets').flush(actifs);
    await fixture.whenStable();
  }

  it('annonce le chargement avant la réponse de l’API', () => {
    const fixture = creer();

    expect(texte(fixture)).toContain('Chargement');
    controleur.expectOne('/api/v1/assets').flush([]);
  });

  it("affiche l'état vide (avec le lien d'enregistrement) quand l'inventaire est réellement vide", async () => {
    const fixture = creer();
    await creerAvecActifs(fixture, []);

    expect(texte(fixture)).toContain('Aucun actif enregistré');
    const lien = element(fixture).querySelector<HTMLAnchorElement>('a[href="/assets/nouveau"]');
    expect(lien).not.toBeNull();
  });

  it('affiche une erreur normalisée sans le détail technique', async () => {
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
    expect(rendu).not.toContain('Détail technique interne.');
    expect(element(fixture).querySelector('[role="alert"]')).not.toBeNull();
  });

  it("affiche l'inventaire reçu, avec un lien vers la fiche de chaque actif", async () => {
    const fixture = creer();
    await creerAvecActifs(fixture, [SERVEUR]);

    const rendu = texte(fixture);
    expect(rendu).toContain('Serveur de sauvegarde');
    expect(rendu).toContain('SRV-00042');
    expect(rendu).toContain('En service');

    const lien = element(fixture).querySelector<HTMLAnchorElement>(
      `a[href="/assets/${SERVEUR.id}"]`,
    );
    expect(lien?.textContent?.trim()).toBe(SERVEUR.name);
  });

  describe('Filtrage local', () => {
    async function selectionner(
      fixture: ComponentFixture<Inventaire>,
      libelleChamp: string,
      valeur: string,
    ): Promise<void> {
      const champs = [...element(fixture).querySelectorAll<HTMLSelectElement>('select')];
      const champ = champs.find(
        (candidat) =>
          element(fixture).querySelector(`label[for="${candidat.id}"]`)?.textContent?.trim() ===
          libelleChamp,
      );
      if (champ === undefined) {
        throw new Error(`Champ de filtre "${libelleChamp}" introuvable.`);
      }
      champ.value = valeur;
      champ.dispatchEvent(new Event('change'));
      await fixture.whenStable();
    }

    it('ne change pas la liste tant qu’aucun filtre n’est appliqué', async () => {
      const fixture = creer();
      await creerAvecActifs(fixture, [SERVEUR, PORTABLE_EN_PANNE]);

      const rendu = texte(fixture);
      expect(rendu).toContain('Serveur de sauvegarde');
      expect(rendu).toContain('Portable support N3');
    });

    it('filtre par type sans appel réseau supplémentaire', async () => {
      const fixture = creer();
      await creerAvecActifs(fixture, [SERVEUR, PORTABLE_EN_PANNE]);

      await selectionner(fixture, 'Type', 'Laptop');

      const rendu = texte(fixture);
      expect(rendu).not.toContain('Serveur de sauvegarde');
      expect(rendu).toContain('Portable support N3');
      controleur.expectNone('/api/v1/assets');
    });

    it('filtre par état, et affiche le message dédié si plus aucun actif ne correspond', async () => {
      const fixture = creer();
      await creerAvecActifs(fixture, [SERVEUR, PORTABLE_EN_PANNE]);

      await selectionner(fixture, 'État', 'Decommissioned');

      const rendu = texte(fixture);
      expect(rendu).not.toContain('Serveur de sauvegarde');
      expect(rendu).not.toContain('Portable support N3');
      expect(rendu).toContain('Aucun actif ne correspond à ces filtres.');
    });
  });

  it(
    'reflète un actif ajouté par InventaireService.ajouterActif sans second appel réseau ' +
      '(critère P-01 : mise à jour depuis la réponse 201, pas par rechargement)',
    async () => {
      const fixture = creer();
      await creerAvecActifs(fixture, [SERVEUR]);

      TestBed.inject(InventaireService).ajouterActif(PORTABLE_EN_PANNE);
      await fixture.whenStable();

      expect(texte(fixture)).toContain('Portable support N3');
      controleur.expectNone('/api/v1/assets');
    },
  );
});
