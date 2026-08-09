import { OptionSelecteur } from '../ui/select-field/select-field';

/**
 * Construit les options d'`app-select-field` à partir d'une table de libellés, dans l'ordre de
 * `valeurs`.
 *
 * Objectif : qu'aucun écran ne recopie à la main les libellés d'une énumération pour peupler un
 * sélecteur. La table de `shared/i18n/libelles.ts` reste ainsi l'unique source, exhaustive par
 * construction.
 *
 * @param valeurs Valeurs à proposer, dans l'ordre d'affichage souhaité (ex. `ASSET_TYPES`).
 * @param libelles Table exhaustive `Record<T, string>` (ex. `LIBELLES_ASSET_TYPE`).
 */
export function optionsDepuisLibelles<T extends string>(
  valeurs: readonly T[],
  libelles: Readonly<Record<T, string>>,
): OptionSelecteur<T>[] {
  return valeurs.map((valeur) => ({ valeur, libelle: libelles[valeur] }));
}
