import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Breadcrumb, EtapeFilAriane } from './breadcrumb';

const ETAPES: readonly EtapeFilAriane[] = [
  { libelle: 'Inventaire', lien: '/assets' },
  { libelle: 'Serveur de sauvegarde', lien: '/assets/a1' },
  { libelle: 'Incident' },
];

describe('Breadcrumb', () => {
  async function creer() {
    const fixture = TestBed.createComponent(Breadcrumb);
    fixture.componentRef.setInput('etapes', ETAPES);
    await fixture.whenStable();
    return fixture;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Breadcrumb],
      providers: [provideRouter([])],
    });
  });

  it('nomme la navigation, pour la distinguer des autres de la page', async () => {
    const fixture = await creer();
    const nav = (fixture.nativeElement as HTMLElement).querySelector('nav');

    expect(nav?.getAttribute('aria-label')).toBe("Fil d'Ariane");
    expect(nav?.querySelector('ol')).not.toBeNull();
  });

  it('rend de vraies ancres pour les étapes précédentes', async () => {
    const fixture = await creer();
    const liens = (fixture.nativeElement as HTMLElement).querySelectorAll('a');

    expect(liens).toHaveLength(2);
    expect(liens[0].getAttribute('href')).toBe('/assets');
  });

  it('marque la dernière étape comme page courante, sans en faire un lien', async () => {
    const fixture = await creer();
    const rendu = fixture.nativeElement as HTMLElement;
    const courante = rendu.querySelector('[aria-current="page"]');

    expect(courante?.textContent?.trim()).toBe('Incident');
    expect(courante?.tagName).toBe('SPAN');
  });

  it('rend les séparateurs décoratifs, pour ne pas les faire énoncer', async () => {
    const fixture = await creer();
    const separateurs = (fixture.nativeElement as HTMLElement).querySelectorAll(
      '[aria-hidden="true"]',
    );

    // Un séparateur entre chaque étape, donc un de moins que le nombre d'étapes.
    expect(separateurs).toHaveLength(ETAPES.length - 1);
  });
});
