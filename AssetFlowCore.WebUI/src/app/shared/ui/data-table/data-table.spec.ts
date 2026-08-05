import { Component, TemplateRef, computed, signal, viewChild } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ColonneTable, DataTable } from './data-table';

interface Actif {
  readonly id: string;
  readonly nom: string;
  readonly serie: string;
  readonly statut: string;
}

const ACTIFS: readonly Actif[] = [
  { id: 'a1', nom: 'Serveur de sauvegarde', serie: 'SRV-00042', statut: 'En service' },
  { id: 'a2', nom: 'Portable RH', serie: 'LAP-00099', statut: 'En panne' },
];

@Component({
  imports: [DataTable],
  template: `
    <ng-template #gabaritStatut let-actif>
      <span class="badge-simule">{{ actif.statut }}</span>
    </ng-template>

    <app-data-table
      [lignes]="lignes()"
      [colonnes]="colonnes()"
      [cleLigne]="cleLigne"
      legende="Inventaire des actifs"
      messageVide="Aucun actif enregistré."
    />
  `,
})
class HoteTable {
  readonly lignes = signal<readonly Actif[]>(ACTIFS);
  readonly cleLigne = (actif: Actif): string => actif.id;

  private readonly gabaritStatut =
    viewChild.required<TemplateRef<{ $implicit: Actif }>>('gabaritStatut');

  readonly colonnes = computed<readonly ColonneTable<Actif>[]>(() => [
    { cle: 'nom', entete: 'Libellé', valeur: (actif) => actif.nom },
    { cle: 'serie', entete: 'Numéro de série', valeur: (actif) => actif.serie },
    {
      cle: 'statut',
      entete: 'État',
      valeur: (actif) => actif.statut,
      // Rendu personnalisé : c'est ainsi qu'un écran place un badge dans une cellule.
      gabarit: this.gabaritStatut(),
      masquerEnCarte: true,
    },
  ]);
}

describe('DataTable', () => {
  let fixture: ComponentFixture<HoteTable>;

  function element(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({ imports: [HoteTable] });
    fixture = TestBed.createComponent(HoteTable);
    await fixture.whenStable();
  });

  it('rend des en-têtes de colonne portés par des th à portée déclarée', () => {
    const entetes = [...element().querySelectorAll('th')];

    expect(entetes.map((th) => th.textContent?.trim())).toEqual([
      'Libellé',
      'Numéro de série',
      'État',
    ]);
    expect(entetes.every((th) => th.getAttribute('scope') === 'col')).toBe(true);
  });

  it('rend une ligne par élément, avec les valeurs des colonnes', () => {
    const lignes = element().querySelectorAll('tbody tr');
    expect(lignes).toHaveLength(2);
    expect(lignes[0].textContent).toContain('Serveur de sauvegarde');
    expect(lignes[0].textContent).toContain('SRV-00042');
  });

  it('nomme la table par une légende, masquée visuellement mais lue', () => {
    const legende = element().querySelector('caption');
    expect(legende?.textContent?.trim()).toBe('Inventaire des actifs');
    expect(legende?.className).toContain('sr-only');
  });

  it('rend la zone de défilement horizontal atteignable au clavier et nommée', () => {
    const region = element().querySelector('[role="region"]');
    expect(region?.getAttribute('tabindex')).toBe('0');
    expect(region?.getAttribute('aria-label')).toBe('Inventaire des actifs');
    expect(region?.className).toContain('overflow-x-auto');
  });

  it('double le rendu en vue de cartes pour les petits écrans, en excluant les colonnes marquées', () => {
    const cartes = element().querySelectorAll('ul > li');
    expect(cartes).toHaveLength(2);

    // Chaque carte reprend les intitulés en liste de définitions…
    const intitules = [...cartes[0].querySelectorAll('dt')].map((dt) => dt.textContent?.trim());
    expect(intitules).toEqual(['Libellé', 'Numéro de série']);
    // …et la colonne « État » en est absente (masquerEnCarte).
    expect(intitules).not.toContain('État');
  });

  it("confie le rendu d'une cellule au gabarit fourni par l'appelant", () => {
    const cellules = element().querySelectorAll('tbody .badge-simule');

    expect(cellules).toHaveLength(2);
    expect(cellules[0].textContent?.trim()).toBe('En service');
  });

  it("affiche le message d'absence de données dans les deux rendus", async () => {
    fixture.componentInstance.lignes.set([]);
    await fixture.whenStable();

    expect(element().querySelector('tbody')?.textContent).toContain('Aucun actif enregistré.');
    expect(element().querySelector('ul')?.textContent).toContain('Aucun actif enregistré.');
    // La cellule vide couvre toute la largeur de la table.
    expect(element().querySelector('tbody td')?.getAttribute('colspan')).toBe('3');
  });
});
