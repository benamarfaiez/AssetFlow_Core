import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Card } from './card/card';
import { EmptyState } from './empty-state/empty-state';
import { ErrorMessage } from './error-message/error-message';
import { NotificationList, NotificationUi } from './notification-list/notification-list';
import { Spinner } from './spinner/spinner';

/*
 * Tests des composants d'état et de la carte. Regroupés : ils partagent une même exigence — ne
 * jamais faire reposer une information sur la seule apparence.
 */

describe('Spinner', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [Spinner] }));

  it('annonce son état par un libellé, jamais par la seule rotation', async () => {
    const fixture = TestBed.createComponent(Spinner);
    fixture.componentRef.setInput('libelle', 'Interrogation de l’API');
    await fixture.whenStable();

    const rendu = fixture.nativeElement as HTMLElement;
    expect(rendu.querySelector('[role="status"]')).not.toBeNull();
    expect(rendu.textContent).toContain('Interrogation');
    // Le cercle animé est décoratif.
    expect(rendu.querySelector('.animate-spin')?.getAttribute('aria-hidden')).toBe('true');
  });

  it("garde le libellé lisible par un lecteur d'écran même masqué visuellement", async () => {
    const fixture = TestBed.createComponent(Spinner);
    fixture.componentRef.setInput('libelleVisible', false);
    await fixture.whenStable();

    const libelle = (fixture.nativeElement as HTMLElement).querySelector('.sr-only');
    expect(libelle?.textContent).toContain('Chargement');
  });
});

describe('EmptyState', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [EmptyState] }));

  it('énonce le constat et sa précision', async () => {
    const fixture = TestBed.createComponent(EmptyState);
    fixture.componentRef.setInput('titre', 'Aucun incident en cours');
    fixture.componentRef.setInput('description', 'Les incidents clôturés restent consultables.');
    await fixture.whenStable();

    const texte = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(texte).toContain('Aucun incident en cours');
    expect(texte).toContain('restent consultables');
  });

  it("n'impose aucun niveau de titre à l'écran hôte", async () => {
    const fixture = TestBed.createComponent(EmptyState);
    fixture.componentRef.setInput('titre', 'Aucun actif');
    await fixture.whenStable();

    const rendu = fixture.nativeElement as HTMLElement;
    expect(rendu.querySelector('h1, h2, h3, h4, h5, h6')).toBeNull();
  });
});

describe('ErrorMessage', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [ErrorMessage] }));

  it("annonce l'échec dès son apparition", async () => {
    const fixture = TestBed.createComponent(ErrorMessage);
    fixture.componentRef.setInput('message', 'Le service est momentanément indisponible.');
    await fixture.whenStable();

    const alerte = (fixture.nativeElement as HTMLElement).querySelector('[role="alert"]');
    expect(alerte?.textContent).toContain('momentanément indisponible');
  });

  it("affiche l'identifiant de trace, qui n'expose aucun détail technique", async () => {
    const fixture = TestBed.createComponent(ErrorMessage);
    fixture.componentRef.setInput('message', 'Une erreur est survenue.');
    fixture.componentRef.setInput('traceId', '0HN7ABCDEF:00000003');
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('0HN7ABCDEF:00000003');
  });

  it('ne propose une nouvelle tentative que si elle a du sens', async () => {
    const fixture = TestBed.createComponent(ErrorMessage);
    fixture.componentRef.setInput('message', 'Échec du chargement.');
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).querySelector('button')).toBeNull();

    fixture.componentRef.setInput('reessayable', true);
    await fixture.whenStable();

    const emis: number[] = [];
    fixture.componentInstance.reessai.subscribe(() => emis.push(1));
    (fixture.nativeElement as HTMLElement).querySelector('button')?.click();

    expect(emis).toHaveLength(1);
  });
});

describe('NotificationList', () => {
  const NOTIFICATIONS: readonly NotificationUi[] = [
    { id: 'n1', tonalite: 'succes', titre: 'Actif enregistré', message: 'SRV-00042 ajouté.' },
    { id: 'n2', tonalite: 'danger', titre: 'Échec', message: 'Le transfert a été refusé.' },
  ];

  beforeEach(() => TestBed.configureTestingModule({ imports: [NotificationList] }));

  async function creer(notifications: readonly NotificationUi[]) {
    const fixture = TestBed.createComponent(NotificationList);
    fixture.componentRef.setInput('notifications', notifications);
    await fixture.whenStable();
    return fixture;
  }

  it("expose une région annoncée présente même vide — un lecteur d'écran n'annonce que les régions préexistantes", async () => {
    const fixture = await creer([]);
    const region = (fixture.nativeElement as HTMLElement).querySelector('[aria-live="polite"]');

    expect(region).not.toBeNull();
  });

  it('rend une notification par élément, avec son intitulé', async () => {
    const fixture = await creer(NOTIFICATIONS);
    const texte = (fixture.nativeElement as HTMLElement).textContent ?? '';

    expect(texte).toContain('Actif enregistré');
    expect(texte).toContain('Le transfert a été refusé.');
  });

  it("nomme chaque bouton de fermeture et émet l'identifiant concerné", async () => {
    const fixture = await creer(NOTIFICATIONS);
    const emis: string[] = [];
    fixture.componentInstance.rejet.subscribe((id) => emis.push(id));

    const boutons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    expect(boutons[1].getAttribute('aria-label')).toBe('Fermer la notification : Échec');
    boutons[1].click();

    expect(emis).toEqual(['n2']);
  });
});

@Component({
  imports: [Card],
  template: `
    <app-card ariaLabel="Fiche de l'actif">
      <h2 slot="entete">Serveur de sauvegarde</h2>
      <p>Numéro de série : SRV-00042</p>
      <button slot="actions" type="button">Mettre au rebut</button>
    </app-card>
  `,
})
class HoteCarte {}

describe('Card', () => {
  beforeEach(() => TestBed.configureTestingModule({ imports: [HoteCarte] }));

  it('projette en-tête, contenu et actions', async () => {
    const fixture = TestBed.createComponent(HoteCarte);
    await fixture.whenStable();

    const rendu = fixture.nativeElement as HTMLElement;
    expect(rendu.querySelector('h2')?.textContent).toContain('Serveur de sauvegarde');
    expect(rendu.textContent).toContain('SRV-00042');
    expect(rendu.querySelector('button')?.textContent).toContain('Mettre au rebut');
  });

  it('ne devient une région nommée que si un nom accessible est fourni', async () => {
    const fixture = TestBed.createComponent(HoteCarte);
    await fixture.whenStable();

    const section = (fixture.nativeElement as HTMLElement).querySelector('section');
    expect(section?.getAttribute('role')).toBe('group');
    expect(section?.getAttribute('aria-label')).toBe("Fiche de l'actif");
  });
});
