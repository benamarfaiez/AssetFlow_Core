import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfirmDialog, TonaliteConfirmation } from './confirm-dialog';

/**
 * Hôte réaliste, à l'image de `modal.spec.ts` : un déclencheur focusable, la boîte de
 * confirmation, et un contenu projeté représentant un champ de motif fourni par l'appelant.
 *
 * `ouverte`, `tonalite`, `enCours` et les libellés sont des **signaux** : en mode zoneless,
 * muter une propriété simple ne provoquerait aucun rendu.
 */
@Component({
  imports: [ConfirmDialog],
  template: `
    <button type="button" id="declencheur" (click)="ouverte.set(true)">Ouvrir</button>

    <app-confirm-dialog
      [ouverte]="ouverte()"
      titre="Mettre l'actif au rebut"
      message="Cette action est irréversible."
      [libelleConfirmation]="libelleConfirmation()"
      [libelleAnnulation]="libelleAnnulation()"
      [tonalite]="tonalite()"
      [enCours]="enCours()"
      (confirmation)="confirmations.set(confirmations() + 1)"
      (annulation)="annulations.set(annulations() + 1)"
    >
      <p id="contenu-projete">Motif projeté par l'appelant</p>
    </app-confirm-dialog>
  `,
})
class HoteConfirmDialog {
  readonly ouverte = signal(false);
  readonly libelleConfirmation = signal('Confirmer');
  readonly libelleAnnulation = signal('Annuler');
  readonly tonalite = signal<TonaliteConfirmation>('danger');
  readonly enCours = signal(false);
  readonly confirmations = signal(0);
  readonly annulations = signal(0);
}

describe('ConfirmDialog', () => {
  let fixture: ComponentFixture<HoteConfirmDialog>;

  function element(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function dialogue(): HTMLElement | null {
    return element().querySelector('[role="dialog"]');
  }

  /** Les boutons projetés sont des `<app-button>` : on cible le `<button>` natif rendu dedans. */
  function boutonParLibelle(libelle: string): HTMLButtonElement {
    const trouve = [
      ...element().querySelectorAll<HTMLButtonElement>('[role="dialog"] button'),
    ].find((bouton) => bouton.textContent?.trim() === libelle);
    if (trouve === undefined) {
      throw new Error(`Bouton "${libelle}" introuvable.`);
    }
    return trouve;
  }

  async function ouvrir(): Promise<void> {
    fixture.componentInstance.ouverte.set(true);
    await fixture.whenStable();
    // Comme pour `Modal` : le piège de focus du CDK capture le focus au rendu suivant celui qui
    // a ouvert le dialogue.
    await fixture.whenStable();
  }

  const geometrieInitiale = Element.prototype.getClientRects;

  beforeEach(async () => {
    // jsdom ne calcule aucune géométrie : sans ce stub, le CDK conclurait qu'aucun élément n'est
    // focusable et renoncerait à déplacer le focus (voir `modal.spec.ts`).
    Element.prototype.getClientRects = function (): DOMRectList {
      return [{}] as unknown as DOMRectList;
    };

    TestBed.configureTestingModule({ imports: [HoteConfirmDialog] });
    fixture = TestBed.createComponent(HoteConfirmDialog);
    await fixture.whenStable();
  });

  afterEach(() => {
    Element.prototype.getClientRects = geometrieInitiale;
  });

  it("relaie le titre et le message à la modale : le message est lu dès l'ouverture, sans région aria-live séparée", async () => {
    await ouvrir();

    const boite = dialogue();
    const idTitre = boite?.getAttribute('aria-labelledby');
    expect(element().querySelector(`#${idTitre}`)?.textContent).toContain('rebut');

    const idDescription = boite?.getAttribute('aria-describedby');
    expect(idDescription).not.toBeNull();
    expect(element().querySelector(`#${idDescription}`)?.textContent).toContain('irréversible');

    expect(element().querySelector('[aria-live]')).toBeNull();
  });

  it('affiche le contenu projeté entre le message et les actions', async () => {
    await ouvrir();

    expect(element().querySelector('#contenu-projete')?.textContent).toContain('Motif projeté');
  });

  it('émet confirmation au clic sur le bouton de confirmation', async () => {
    await ouvrir();

    boutonParLibelle('Confirmer').click();
    await fixture.whenStable();

    expect(fixture.componentInstance.confirmations()).toBe(1);
    expect(fixture.componentInstance.annulations()).toBe(0);
  });

  it("émet annulation au clic sur le bouton d'annulation", async () => {
    await ouvrir();

    boutonParLibelle('Annuler').click();
    await fixture.whenStable();

    expect(fixture.componentInstance.annulations()).toBe(1);
    expect(fixture.componentInstance.confirmations()).toBe(0);
  });

  it('émet annulation sur Échap', async () => {
    await ouvrir();

    dialogue()?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await fixture.whenStable();

    expect(fixture.componentInstance.annulations()).toBe(1);
  });

  it('ignore Échap et désactive les deux actions pendant un traitement en cours', async () => {
    await ouvrir();
    const annulerBtn = boutonParLibelle('Annuler');
    const confirmerBtn = boutonParLibelle('Confirmer');

    fixture.componentInstance.enCours.set(true);
    await fixture.whenStable();

    expect(annulerBtn.disabled).toBe(true);
    expect(confirmerBtn.disabled).toBe(true);
    expect(confirmerBtn.getAttribute('aria-busy')).toBe('true');

    dialogue()?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await fixture.whenStable();

    expect(fixture.componentInstance.annulations()).toBe(0);
    expect(fixture.componentInstance.ouverte()).toBe(true);
  });

  it("choisit la variante 'danger' par défaut, et 'primaire' pour un avertissement", async () => {
    await ouvrir();

    expect(boutonParLibelle('Confirmer').className).toContain('bg-danger');

    fixture.componentInstance.tonalite.set('avertissement');
    await fixture.whenStable();

    expect(boutonParLibelle('Confirmer').className).toContain('bg-primaire');
  });

  it('affiche les libellés personnalisés des actions', async () => {
    fixture.componentInstance.libelleConfirmation.set('Mettre au rebut');
    fixture.componentInstance.libelleAnnulation.set('Garder en service');
    await ouvrir();

    expect(boutonParLibelle('Mettre au rebut')).not.toBeNull();
    expect(boutonParLibelle('Garder en service')).not.toBeNull();
  });
});
