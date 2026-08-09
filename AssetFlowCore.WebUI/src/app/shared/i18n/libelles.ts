import { AssetStatus, AssetType } from '../models/asset.model';
import { TicketCriticality, TicketStatus } from '../models/ticket.model';

/*
 * Traduction des valeurs d'énumérations de l'API.
 *
 * L'API transporte ces valeurs **en anglais** (`InService`, `NetworkDevice`, `High`) : aucune
 * ne doit s'afficher telle quelle (exigence `ENF-22`). Les tables sont typées
 * `Record<Union, string>` et donc **exhaustives par construction** : si le contrat gagne une
 * valeur d'énumération, la compilation échoue ici tant qu'elle n'est pas traduite — c'est
 * exactement le rappel que l'on veut, plutôt qu'un libellé manquant découvert à l'écran.
 */

/** Libellés des types d'actifs. */
export const LIBELLES_ASSET_TYPE: Readonly<Record<AssetType, string>> = {
  Server: $localize`:@@libelles.assetType.server:Serveur`,
  Laptop: $localize`:@@libelles.assetType.laptop:Ordinateur portable`,
  NetworkDevice: $localize`:@@libelles.assetType.networkDevice:Équipement réseau`,
};

/** Libellés des états d'un actif. */
export const LIBELLES_ASSET_STATUS: Readonly<Record<AssetStatus, string>> = {
  InService: $localize`:@@libelles.assetStatus.inService:En service`,
  Down: $localize`:@@libelles.assetStatus.down:En panne`,
  InMaintenance: $localize`:@@libelles.assetStatus.inMaintenance:En maintenance`,
  Decommissioned: $localize`:@@libelles.assetStatus.decommissioned:Mis au rebut`,
};

/** Libellés des états d'un incident. */
export const LIBELLES_TICKET_STATUS: Readonly<Record<TicketStatus, string>> = {
  Opened: $localize`:@@libelles.ticketStatus.opened:Ouvert`,
  InProgress: $localize`:@@libelles.ticketStatus.inProgress:En cours`,
  Closed: $localize`:@@libelles.ticketStatus.closed:Clôturé`,
};

/** Libellés des niveaux de criticité d'un incident. */
export const LIBELLES_TICKET_CRITICALITY: Readonly<Record<TicketCriticality, string>> = {
  Low: $localize`:@@libelles.ticketCriticality.low:Faible`,
  Medium: $localize`:@@libelles.ticketCriticality.medium:Moyenne`,
  High: $localize`:@@libelles.ticketCriticality.high:Haute`,
};

/**
 * Traduit une valeur, en se rabattant sur la valeur brute si la table ne la connaît pas.
 *
 * Le repli ne devrait jamais servir — les tables sont exhaustives — mais il évite qu'un
 * `undefined` s'affiche si l'API renvoyait une valeur inconnue du contrat compilé.
 */
export function traduire<T extends string>(table: Readonly<Record<T, string>>, valeur: T): string {
  return table[valeur] ?? valeur;
}
