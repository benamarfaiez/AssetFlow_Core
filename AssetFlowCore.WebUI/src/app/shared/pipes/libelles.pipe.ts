import { Pipe, PipeTransform } from '@angular/core';
import {
  LIBELLES_ASSET_STATUS,
  LIBELLES_ASSET_TYPE,
  LIBELLES_TICKET_CRITICALITY,
  LIBELLES_TICKET_STATUS,
  traduire,
} from '../i18n/libelles';
import { AssetStatus, AssetType } from '../models/asset.model';
import { TicketCriticality, TicketStatus } from '../models/ticket.model';

/*
 * Un pipe par énumération plutôt qu'un pipe générique prenant un domaine en paramètre : la
 * valeur reste typée, et une confusion de domaine (`'High' | libelleAssetStatus`) devient une
 * erreur de compilation au lieu d'un libellé vide à l'écran.
 *
 * Les pipes sont purs : ils ne recalculent que si la valeur change.
 */

/** `{{ actif.type | libelleAssetType }}` → « Serveur ». */
@Pipe({ name: 'libelleAssetType' })
export class AssetTypeLabelPipe implements PipeTransform {
  transform(valeur: AssetType): string {
    return traduire(LIBELLES_ASSET_TYPE, valeur);
  }
}

/** `{{ actif.status | libelleAssetStatus }}` → « En service ». */
@Pipe({ name: 'libelleAssetStatus' })
export class AssetStatusLabelPipe implements PipeTransform {
  transform(valeur: AssetStatus): string {
    return traduire(LIBELLES_ASSET_STATUS, valeur);
  }
}

/** `{{ incident.status | libelleTicketStatus }}` → « En cours ». */
@Pipe({ name: 'libelleTicketStatus' })
export class TicketStatusLabelPipe implements PipeTransform {
  transform(valeur: TicketStatus): string {
    return traduire(LIBELLES_TICKET_STATUS, valeur);
  }
}

/** `{{ incident.criticality | libelleTicketCriticality }}` → « Haute ». */
@Pipe({ name: 'libelleTicketCriticality' })
export class TicketCriticalityLabelPipe implements PipeTransform {
  transform(valeur: TicketCriticality): string {
    return traduire(LIBELLES_TICKET_CRITICALITY, valeur);
  }
}
