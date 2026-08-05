import { TestBed } from '@angular/core/testing';
import { ASSET_STATUSES } from '../../models/asset.model';
import { TICKET_CRITICALITIES, TICKET_STATUSES } from '../../models/ticket.model';
import { Badge } from '../badge/badge';
import { AssetStatusBadge, TicketCriticalityBadge, TicketStatusBadge } from './status-badges';

describe('Badge', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [Badge] });
  });

  it("porte toujours son libellé : l'information ne repose jamais sur la couleur", async () => {
    const fixture = TestBed.createComponent(Badge);
    fixture.componentRef.setInput('libelle', 'En panne');
    fixture.componentRef.setInput('tonalite', 'danger');
    await fixture.whenStable();

    const rendu = fixture.nativeElement as HTMLElement;
    expect(rendu.textContent).toContain('En panne');
    expect(rendu.querySelector('span')?.className).toContain('bg-danger-fond');
  });

  it('marque la pastille comme décorative', async () => {
    const fixture = TestBed.createComponent(Badge);
    fixture.componentRef.setInput('libelle', 'Ouvert');
    await fixture.whenStable();

    const pastille = (fixture.nativeElement as HTMLElement).querySelector('[aria-hidden="true"]');
    expect(pastille).not.toBeNull();
  });
});

describe('badges du domaine', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AssetStatusBadge, TicketStatusBadge, TicketCriticalityBadge],
    });
  });

  it("traduit l'état d'un actif", async () => {
    const fixture = TestBed.createComponent(AssetStatusBadge);
    fixture.componentRef.setInput('statut', 'InMaintenance');
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('En maintenance');
  });

  it("traduit l'état d'un incident", async () => {
    const fixture = TestBed.createComponent(TicketStatusBadge);
    fixture.componentRef.setInput('statut', 'InProgress');
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('En cours');
  });

  it('traduit la criticité et la distingue par la tonalité', async () => {
    const fixture = TestBed.createComponent(TicketCriticalityBadge);
    fixture.componentRef.setInput('criticite', 'High');
    await fixture.whenStable();

    const rendu = fixture.nativeElement as HTMLElement;
    expect(rendu.textContent).toContain('Haute');
    expect(rendu.querySelector('span')?.className).toContain('danger');
  });

  it('rend une valeur non vide et traduite pour **toutes** les valeurs du contrat', async () => {
    for (const statut of ASSET_STATUSES) {
      const fixture = TestBed.createComponent(AssetStatusBadge);
      fixture.componentRef.setInput('statut', statut);
      await fixture.whenStable();
      const texte = (fixture.nativeElement as HTMLElement).textContent?.trim() ?? '';
      expect(texte, `état d'actif non rendu : ${statut}`).not.toBe('');
      expect(texte, `état d'actif laissé en anglais : ${statut}`).not.toBe(statut);
    }

    for (const statut of TICKET_STATUSES) {
      const fixture = TestBed.createComponent(TicketStatusBadge);
      fixture.componentRef.setInput('statut', statut);
      await fixture.whenStable();
      const texte = (fixture.nativeElement as HTMLElement).textContent?.trim() ?? '';
      expect(texte, `état d'incident non rendu : ${statut}`).not.toBe('');
      expect(texte, `état d'incident laissé en anglais : ${statut}`).not.toBe(statut);
    }

    for (const criticite of TICKET_CRITICALITIES) {
      const fixture = TestBed.createComponent(TicketCriticalityBadge);
      fixture.componentRef.setInput('criticite', criticite);
      await fixture.whenStable();
      const texte = (fixture.nativeElement as HTMLElement).textContent?.trim() ?? '';
      expect(texte, `criticité non rendue : ${criticite}`).not.toBe('');
      expect(texte, `criticité laissée en anglais : ${criticite}`).not.toBe(criticite);
    }
  });
});
