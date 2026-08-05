import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Button } from './button';

describe('Button', () => {
  let fixture: ComponentFixture<Button>;

  async function creer(): Promise<void> {
    fixture = TestBed.createComponent(Button);
    await fixture.whenStable();
  }

  function bouton(): HTMLButtonElement {
    const trouve = (fixture.nativeElement as HTMLElement).querySelector('button');
    if (trouve === null) {
      throw new Error("Le bouton natif n'a pas été rendu.");
    }
    return trouve;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [Button] });
  });

  it('rend un bouton natif de type button par défaut, jamais un submit involontaire', async () => {
    await creer();
    expect(bouton().type).toBe('button');
  });

  it('devient inactif et annonce son occupation pendant une action longue', async () => {
    await creer();
    fixture.componentRef.setInput('enCours', true);
    await fixture.whenStable();

    expect(bouton().disabled).toBe(true);
    expect(bouton().getAttribute('aria-busy')).toBe('true');
    // L'indicateur porte un libellé lu par les lecteurs d'écran.
    expect(bouton().textContent).toContain('Traitement en cours');
  });

  it("n'annonce aucune occupation au repos", async () => {
    await creer();
    expect(bouton().getAttribute('aria-busy')).toBeNull();
  });

  it("respecte l'état désactivé demandé", async () => {
    await creer();
    fixture.componentRef.setInput('disabled', true);
    await fixture.whenStable();

    expect(bouton().disabled).toBe(true);
  });

  it('accepte un nom accessible explicite, pour un bouton sans libellé visible', async () => {
    await creer();
    fixture.componentRef.setInput('ariaLabel', "Supprimer l'équipe Réseau");
    await fixture.whenStable();

    expect(bouton().getAttribute('aria-label')).toBe("Supprimer l'équipe Réseau");
  });

  it('atteint la cible tactile minimale et change de style selon la variante', async () => {
    await creer();
    const classesPrimaire = bouton().className;
    expect(classesPrimaire).toContain('min-h-(--cible-tactile)');
    expect(classesPrimaire).toContain('bg-primaire');

    fixture.componentRef.setInput('variante', 'danger');
    await fixture.whenStable();
    expect(bouton().className).toContain('bg-danger');
  });
});
