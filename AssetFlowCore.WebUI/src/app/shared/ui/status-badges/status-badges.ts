import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import {
  LIBELLES_ASSET_STATUS,
  LIBELLES_TICKET_CRITICALITY,
  LIBELLES_TICKET_STATUS,
  traduire,
} from '../../i18n/libelles';
import { AssetStatus } from '../../models/asset.model';
import { TicketCriticality, TicketStatus } from '../../models/ticket.model';
import { Badge } from '../badge/badge';
import { Tonalite } from '../tonalite';

/*
 * Badges propres au domaine : ils encapsulent **la traduction du libellé et le choix de la
 * tonalité**, pour que ces deux correspondances soient définies une seule fois plutôt que
 * répétées dans chaque écran. Les tables sont exhaustives par construction : une nouvelle valeur
 * d'énumération dans le contrat casse la compilation ici.
 *
 * Ces trois composants délèguent tout le rendu à `Badge` : leur gabarit est donc écrit en ligne,
 * la convention du dépôt (gabarit externe) n'ayant pas d'intérêt pour un unique élément.
 */

const TONALITES_ASSET_STATUS: Readonly<Record<AssetStatus, Tonalite>> = {
  InService: 'succes',
  Down: 'danger',
  InMaintenance: 'alerte',
  Decommissioned: 'neutre',
};

const TONALITES_TICKET_STATUS: Readonly<Record<TicketStatus, Tonalite>> = {
  Opened: 'info',
  InProgress: 'alerte',
  Resolved: 'succes',
  Closed: 'neutre',
};

const TONALITES_TICKET_CRITICALITY: Readonly<Record<TicketCriticality, Tonalite>> = {
  Low: 'neutre',
  Medium: 'alerte',
  High: 'danger',
};

/** État d'un actif : « En service », « En panne », « En maintenance », « Mis au rebut ». */
@Component({
  selector: 'app-asset-status-badge',
  imports: [Badge],
  template: '<app-badge [libelle]="libelle()" [tonalite]="tonalite()" />',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex' },
})
export class AssetStatusBadge {
  readonly statut = input.required<AssetStatus>();

  protected readonly libelle = computed(() => traduire(LIBELLES_ASSET_STATUS, this.statut()));
  protected readonly tonalite = computed<Tonalite>(
    () => TONALITES_ASSET_STATUS[this.statut()] ?? 'neutre',
  );
}

/** État d'un incident : « Ouvert », « En cours », « Résolu », « Clôturé ». */
@Component({
  selector: 'app-ticket-status-badge',
  imports: [Badge],
  template: '<app-badge [libelle]="libelle()" [tonalite]="tonalite()" />',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex' },
})
export class TicketStatusBadge {
  readonly statut = input.required<TicketStatus>();

  protected readonly libelle = computed(() => traduire(LIBELLES_TICKET_STATUS, this.statut()));
  protected readonly tonalite = computed<Tonalite>(
    () => TONALITES_TICKET_STATUS[this.statut()] ?? 'neutre',
  );
}

/**
 * Criticité d'un incident : « Faible », « Moyenne », « Haute ».
 *
 * La tonalité suit la gravité, mais le libellé reste la source d'information : une criticité ne
 * se devine pas à la couleur.
 */
@Component({
  selector: 'app-ticket-criticality-badge',
  imports: [Badge],
  template: '<app-badge [libelle]="libelle()" [tonalite]="tonalite()" />',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex' },
})
export class TicketCriticalityBadge {
  readonly criticite = input.required<TicketCriticality>();

  protected readonly libelle = computed(() =>
    traduire(LIBELLES_TICKET_CRITICALITY, this.criticite()),
  );
  protected readonly tonalite = computed<Tonalite>(
    () => TONALITES_TICKET_CRITICALITY[this.criticite()] ?? 'neutre',
  );
}
