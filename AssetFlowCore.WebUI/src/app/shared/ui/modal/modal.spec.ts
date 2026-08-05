import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Modal } from './modal';

/**
 * Hôte réaliste : un déclencheur focusable, puis la modale — pour vérifier la restitution du focus.
 *
 * `ouverte` est un **signal** : en mode zoneless, muter une propriété simple ne provoquerait
 * aucun rendu. C'est la contrainte que tout appelant de la modale doit respecter.
 */
@Component({
  imports: [Modal],
  template: `
    <button type="button" id="declencheur" (click)="ouverte.set(true)">Ouvrir</button>

    <app-modal
      [ouverte]="ouverte()"
      titre="Mettre l'actif au rebut"
      description="Cette action est irréversible."
      (fermeture)="ouverte.set(false)"
    >
      <p>Confirmez-vous la mise au rebut ?</p>
      <button slot="actions" type="button" id="confirmer">Confirmer</button>
    </app-modal>
  `,
})
class HoteModale {
  readonly ouverte = signal(false);
}

describe('Modal', () => {
  let fixture: ComponentFixture<HoteModale>;

  function element(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function dialogue(): HTMLElement | null {
    return element().querySelector('[role="dialog"]');
  }

  async function ouvrir(): Promise<void> {
    fixture.componentInstance.ouverte.set(true);
    await fixture.whenStable();
    // Le piège de focus du CDK capture le focus dans un `afterNextRender` : la prise d'effet
    // n'intervient donc qu'au rendu **suivant** celui qui a ouvert le dialogue.
    await fixture.whenStable();
  }

  const geometrieInitiale = Element.prototype.getClientRects;

  beforeEach(async () => {
    // jsdom ne calcule aucune géométrie : le CDK conclut alors qu'aucun élément n'est focusable
    // (`hasGeometry` est faux) et renonce à déplacer le focus. Simuler une géométrie non nulle
    // permet d'exercer le comportement réel du piège de focus plutôt que de le contourner.
    Element.prototype.getClientRects = function (): DOMRectList {
      return [{}] as unknown as DOMRectList;
    };

    TestBed.configureTestingModule({ imports: [HoteModale] });
    fixture = TestBed.createComponent(HoteModale);
    await fixture.whenStable();
  });

  afterEach(() => {
    Element.prototype.getClientRects = geometrieInitiale;
  });

  it("ne rend rien tant qu'elle est fermée", () => {
    expect(dialogue()).toBeNull();
  });

  it('annonce un dialogue modal nommé par son titre et décrit par son texte', async () => {
    await ouvrir();

    const boite = dialogue();
    expect(boite).not.toBeNull();
    expect(boite?.getAttribute('aria-modal')).toBe('true');

    const idTitre = boite?.getAttribute('aria-labelledby');
    expect(element().querySelector(`#${idTitre}`)?.textContent).toContain('au rebut');

    const idDescription = boite?.getAttribute('aria-describedby');
    expect(element().querySelector(`#${idDescription}`)?.textContent).toContain('irréversible');
  });

  it("déplace le focus dans le dialogue à l'ouverture", async () => {
    const declencheur = element().querySelector<HTMLButtonElement>('#declencheur');
    declencheur?.focus();
    expect(document.activeElement).toBe(declencheur);

    await ouvrir();

    expect(dialogue()?.contains(document.activeElement)).toBe(true);
  });

  it('restitue le focus au déclencheur à la fermeture', async () => {
    const declencheur = element().querySelector<HTMLButtonElement>('#declencheur');
    declencheur?.focus();
    await ouvrir();

    fixture.componentInstance.ouverte.set(false);
    await fixture.whenStable();

    expect(document.activeElement).toBe(declencheur);
  });

  it('demande sa fermeture sur Échap', async () => {
    await ouvrir();

    dialogue()?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    await fixture.whenStable();

    expect(fixture.componentInstance.ouverte()).toBe(false);
  });

  it('demande sa fermeture au clic sur le bouton dédié, qui porte un nom accessible', async () => {
    await ouvrir();

    const fermer = element().querySelector<HTMLButtonElement>('[aria-label="Fermer"]');
    expect(fermer).not.toBeNull();
    fermer?.click();
    await fixture.whenStable();

    expect(fixture.componentInstance.ouverte()).toBe(false);
  });

  it("ne se ferme pas sur un clic à l'intérieur du panneau", async () => {
    await ouvrir();

    dialogue()?.click();
    await fixture.whenStable();

    expect(fixture.componentInstance.ouverte()).toBe(true);
  });

  it('projette le contenu et les actions', async () => {
    await ouvrir();

    expect(element().textContent).toContain('Confirmez-vous');
    expect(element().querySelector('#confirmer')).not.toBeNull();
  });

  it("empêche le défilement de la page pendant l'affichage, puis le rétablit", async () => {
    await ouvrir();
    expect(document.body.style.overflow).toBe('hidden');

    fixture.componentInstance.ouverte.set(false);
    await fixture.whenStable();
    expect(document.body.style.overflow).not.toBe('hidden');
  });
});
