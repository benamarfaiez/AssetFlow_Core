import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { errorInterceptor } from '../../../core/http/error.interceptor';
import { AssetResponse } from '../../../shared/models/asset.model';
import { InventaireService } from '../inventaire/inventaire.service';
import { Formulaire } from './formulaire';

const ACTIF_CREE: AssetResponse = {
  id: '11111111-1111-1111-1111-111111111111',
  name: 'Serveur de sauvegarde',
  serialNumber: 'SRV-00042',
  type: 'Server',
  status: 'InService',
  createdAt: '2026-08-05T09:00:00Z',
};

describe('Formulaire', () => {
  let controleur: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Formulaire],
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

  function element(fixture: ComponentFixture<Formulaire>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  /**
   * Crée le composant, répond à l'appel incident de `InventaireService` (voir le commentaire de
   * tête de ce service : injecter `Formulaire` instancie ce singleton racine, dont la ressource
   * se déclenche dès sa construction, indépendamment de toute lecture de l'inventaire ici) puis
   * laisse le rendu se stabiliser.
   */
  async function creer(): Promise<ComponentFixture<Formulaire>> {
    const fixture = TestBed.createComponent(Formulaire);
    fixture.detectChanges();

    controleur.expectOne((requete) => requete.method === 'GET').flush([]);
    await fixture.whenStable();

    return fixture;
  }

  function champ(fixture: ComponentFixture<Formulaire>, label: string): HTMLInputElement {
    const trouve = [...element(fixture).querySelectorAll<HTMLLabelElement>('label')].find(
      (candidat) => candidat.textContent?.trim().startsWith(label),
    );
    if (trouve === undefined) {
      throw new Error(`Champ "${label}" introuvable.`);
    }
    const id = trouve.getAttribute('for');
    const input = element(fixture).querySelector<HTMLInputElement>(`#${id}`);
    if (input === null) {
      throw new Error(`Contrôle du champ "${label}" introuvable (id ${id}).`);
    }
    return input;
  }

  function saisir(fixture: ComponentFixture<Formulaire>, label: string, valeur: string): void {
    const input = champ(fixture, label);
    input.value = valeur;
    input.dispatchEvent(new Event('input', { bubbles: true }));
  }

  async function soumettre(fixture: ComponentFixture<Formulaire>): Promise<void> {
    const bouton = element(fixture).querySelector<HTMLButtonElement>('button[type="submit"]');
    if (bouton === null) {
      throw new Error('Bouton de soumission introuvable.');
    }
    bouton.click();
    await fixture.whenStable();
  }

  it('remplit valablement le formulaire, envoie la requête et redirige vers l’inventaire', async () => {
    const fixture = await creer();

    saisir(fixture, 'Libellé', 'Serveur de sauvegarde');
    saisir(fixture, 'Numéro de série', '  srv-00042  ');

    const router = TestBed.inject(Router);
    const navigation = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    await soumettre(fixture);

    const requete = controleur.expectOne((r) => r.method === 'POST');
    expect(requete.request.body).toEqual({
      name: 'Serveur de sauvegarde',
      serialNumber: 'srv-00042',
      type: 'Server',
    });

    requete.flush(ACTIF_CREE, { status: 201, statusText: 'Created' });
    await fixture.whenStable();

    expect(navigation).toHaveBeenCalledWith(['/assets']);
    // Critère P-01 : l'actif rejoint l'inventaire depuis la réponse, numéro normalisé compris.
    expect(TestBed.inject(InventaireService).actifs()).toContainEqual(ACTIF_CREE);
  });

  it('affiche les erreurs de champs sans aucun appel réseau sur un formulaire vide', async () => {
    const fixture = await creer();

    await soumettre(fixture);

    controleur.expectNone((r) => r.method === 'POST');
    expect(element(fixture).textContent).toContain('Ce champ est obligatoire');
  });

  it('refuse localement un numéro de série trop court, sans appel réseau', async () => {
    const fixture = await creer();

    saisir(fixture, 'Libellé', 'Serveur de sauvegarde');
    saisir(fixture, 'Numéro de série', 'ABCD');

    await soumettre(fixture);

    controleur.expectNone((r) => r.method === 'POST');
    expect(element(fixture).textContent).toContain(
      'Le numéro de série doit contenir entre 5 et 50 caractères.',
    );
  });

  it('reporte un doublon de numéro de série sur le champ correspondant', async () => {
    const fixture = await creer();

    saisir(fixture, 'Libellé', 'Serveur de sauvegarde');
    saisir(fixture, 'Numéro de série', 'SRV-00042');

    await soumettre(fixture);

    controleur
      .expectOne((r) => r.method === 'POST')
      .flush(
        {
          title: 'Règle métier violée',
          status: 400,
          detail: 'Ce numéro de série constructeur est déjà enregistré dans le parc.',
        },
        { status: 400, statusText: 'Bad Request' },
      );
    await fixture.whenStable();

    const messageErreur = element(fixture).querySelector('[id$="-erreur"]');
    expect(messageErreur?.textContent).toContain('déjà enregistré');
  });

  it('affiche une erreur métier générique globalement quand elle ne concerne aucun champ', async () => {
    const fixture = await creer();

    saisir(fixture, 'Libellé', 'Serveur de sauvegarde');
    saisir(fixture, 'Numéro de série', 'SRV-00042');

    await soumettre(fixture);

    controleur
      .expectOne((r) => r.method === 'POST')
      .flush(
        { title: 'Règle métier violée', status: 400, detail: 'Une autre règle a été violée.' },
        { status: 400, statusText: 'Bad Request' },
      );
    await fixture.whenStable();

    expect(element(fixture).querySelector('[role="alert"]')?.textContent).toContain(
      'Une autre règle a été violée.',
    );
  });
});
