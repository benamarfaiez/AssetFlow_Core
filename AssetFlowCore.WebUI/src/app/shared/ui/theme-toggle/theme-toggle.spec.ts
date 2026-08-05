import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ThemePrefere, ThemeToggle } from './theme-toggle';

describe('ThemeToggle', () => {
  let fixture: ComponentFixture<ThemeToggle>;

  async function creer(theme: ThemePrefere = 'auto'): Promise<void> {
    fixture = TestBed.createComponent(ThemeToggle);
    fixture.componentRef.setInput('theme', theme);
    await fixture.whenStable();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ThemeToggle] });
  });

  it('propose les trois préférences, dans un groupe nommé', async () => {
    await creer();

    const rendu = fixture.nativeElement as HTMLElement;
    const boutons = rendu.querySelectorAll<HTMLInputElement>('input[type="radio"]');

    expect(boutons).toHaveLength(3);
    expect(rendu.querySelector('legend')?.textContent).toContain('Thème');
    // Les trois boutons partagent un nom de groupe : les flèches naviguent entre eux.
    const noms = new Set([...boutons].map((bouton) => bouton.name));
    expect(noms.size).toBe(1);
  });

  it('reflète la préférence reçue sur le bouton correspondant', async () => {
    await creer('dark');

    const coche = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
      'input:checked',
    );
    expect(coche?.value).toBe('dark');
  });

  it("émet la préférence choisie sans l'appliquer lui-même", async () => {
    await creer('auto');

    const emis: ThemePrefere[] = [];
    fixture.componentInstance.themeChange.subscribe((theme) => emis.push(theme));

    const boutons = (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLInputElement>(
      'input[type="radio"]',
    );
    boutons[2].click();
    await fixture.whenStable();

    expect(emis).toEqual(['dark']);
    // Le composant reste piloté par son entrée : il n'a pas changé d'état de lui-même.
    expect(fixture.componentInstance.theme()).toBe('auto');
  });

  it('associe chaque bouton à un libellé lisible', async () => {
    await creer();

    const libelles = [...(fixture.nativeElement as HTMLElement).querySelectorAll('label')].map(
      (label) => label.textContent?.trim(),
    );

    expect(libelles).toEqual(['Auto', 'Clair', 'Sombre']);
  });
});
