import { LIBELLES_ASSET_TYPE } from '../i18n/libelles';
import { ASSET_TYPES, AssetType } from '../models/asset.model';
import { optionsDepuisLibelles } from './options-depuis-libelles';

describe('optionsDepuisLibelles', () => {
  it("construit les options dans l'ordre des valeurs, avec leurs libellés traduits", () => {
    const options = optionsDepuisLibelles(ASSET_TYPES, LIBELLES_ASSET_TYPE);

    expect(options).toEqual([
      { valeur: 'Server', libelle: 'Serveur' },
      { valeur: 'Laptop', libelle: 'Ordinateur portable' },
      { valeur: 'NetworkDevice', libelle: 'Équipement réseau' },
    ]);
  });

  it("respecte l'ordre du tableau de valeurs fourni, même s'il diffère de celui de la table de libellés", () => {
    const ordrePersonnalise: readonly AssetType[] = ['NetworkDevice', 'Server', 'Laptop'];

    const options = optionsDepuisLibelles(ordrePersonnalise, LIBELLES_ASSET_TYPE);

    expect(options.map((option) => option.valeur)).toEqual(['NetworkDevice', 'Server', 'Laptop']);
  });

  it('produit des options directement compatibles avec un sous-ensemble de valeurs', () => {
    const options = optionsDepuisLibelles(['Server', 'Laptop'] as const, LIBELLES_ASSET_TYPE);

    expect(options).toEqual([
      { valeur: 'Server', libelle: 'Serveur' },
      { valeur: 'Laptop', libelle: 'Ordinateur portable' },
    ]);
  });
});
