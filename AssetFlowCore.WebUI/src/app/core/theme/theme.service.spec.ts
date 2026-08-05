import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

const CLE_STOCKAGE = 'assetflow.theme';

/**
 * Stockage en mémoire conforme à l'interface `Storage`.
 *
 * L'environnement de test ne fournit **pas** `window.localStorage` : jsdom le refuse sur une
 * origine opaque (`about:blank`). C'est d'ailleurs ce que le service doit déjà savoir encaisser
 * en navigation privée — un cas couvert par le dernier test de ce fichier.
 */
function creerStockage(): Storage {
  const donnees = new Map<string, string>();

  return {
    get length() {
      return donnees.size;
    },
    clear: () => donnees.clear(),
    getItem: (cle) => donnees.get(cle) ?? null,
    key: (index) => [...donnees.keys()][index] ?? null,
    removeItem: (cle) => void donnees.delete(cle),
    setItem: (cle, valeur) => void donnees.set(cle, valeur),
  };
}

/** Substitue un stockage à `window.localStorage` et rend de quoi le retirer. */
function installerStockage(stockage: Storage | undefined): () => void {
  const descripteurInitial = Object.getOwnPropertyDescriptor(window, 'localStorage');

  Object.defineProperty(window, 'localStorage', {
    value: stockage,
    configurable: true,
    writable: true,
  });

  return () => {
    if (descripteurInitial === undefined) {
      Reflect.deleteProperty(window, 'localStorage');
    } else {
      Object.defineProperty(window, 'localStorage', descripteurInitial);
    }
  };
}

describe('ThemeService', () => {
  let desinstaller: () => void;

  beforeEach(() => {
    desinstaller = installerStockage(creerStockage());
    document.documentElement.removeAttribute('data-theme');
    TestBed.configureTestingModule({});
  });

  afterEach(() => {
    desinstaller();
    document.documentElement.removeAttribute('data-theme');
  });

  it('démarre en mode automatique et ne pose aucun attribut : le système décide', () => {
    const service = TestBed.inject(ThemeService);
    TestBed.tick();

    expect(service.theme()).toBe('auto');
    expect(document.documentElement.hasAttribute('data-theme')).toBe(false);
  });

  it('force le thème sombre, y compris sous un système clair', () => {
    const service = TestBed.inject(ThemeService);
    service.definir('dark');
    TestBed.tick();

    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('force le thème clair, y compris sous un système sombre — la bascule vaut dans les deux sens', () => {
    const service = TestBed.inject(ThemeService);
    service.definir('light');
    TestBed.tick();

    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
  });

  it("retire l'attribut au retour en mode automatique", () => {
    const service = TestBed.inject(ThemeService);
    service.definir('dark');
    TestBed.tick();
    service.definir('auto');
    TestBed.tick();

    expect(document.documentElement.hasAttribute('data-theme')).toBe(false);
  });

  it('retient la préférence explicite et la restitue à la session suivante', () => {
    const service = TestBed.inject(ThemeService);
    service.definir('dark');
    TestBed.tick();

    expect(window.localStorage.getItem(CLE_STOCKAGE)).toBe('dark');

    // Nouvelle instance, comme au rechargement de l'application.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({});
    const relance = TestBed.inject(ThemeService);

    expect(relance.theme()).toBe('dark');
  });

  it('oublie la préférence au retour en mode automatique', () => {
    const service = TestBed.inject(ThemeService);
    service.definir('light');
    TestBed.tick();
    service.definir('auto');
    TestBed.tick();

    expect(window.localStorage.getItem(CLE_STOCKAGE)).toBeNull();
  });

  it("ignore une valeur de stockage inattendue plutôt que de l'appliquer", () => {
    window.localStorage.setItem(CLE_STOCKAGE, 'fuchsia');

    const service = TestBed.inject(ThemeService);

    expect(service.theme()).toBe('auto');
  });

  it('applique tout de même le thème quand le stockage est refusé — navigation privée', () => {
    desinstaller();
    desinstaller = installerStockage({
      get length(): number {
        throw new Error('Stockage refusé');
      },
      clear: () => {
        throw new Error('Stockage refusé');
      },
      getItem: () => {
        throw new Error('Stockage refusé');
      },
      key: () => {
        throw new Error('Stockage refusé');
      },
      removeItem: () => {
        throw new Error('Stockage refusé');
      },
      setItem: () => {
        throw new Error('Stockage refusé');
      },
    });

    const service = TestBed.inject(ThemeService);
    service.definir('dark');
    TestBed.tick();

    // La préférence n'est pas retenue, mais elle vaut pour la session en cours.
    expect(service.theme()).toBe('dark');
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });
});
