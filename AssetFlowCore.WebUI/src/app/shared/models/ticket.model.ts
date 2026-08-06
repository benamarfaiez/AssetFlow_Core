// Contrat synchronisé depuis le backend .NET — ne pas modifier à la main.
// Sources : AssetFlowCore.Application/DTOs/TicketResponseDto.cs · TicketTransferHistoryDto.cs
//           AssetFlowCore.WebApi/Requests/CreateTicketRequest.cs · CloseTicketRequest.cs
//           AssetFlowCore.WebApi/Requests/TransferTicketRequest.cs
//           AssetFlowCore.Application/UseCases/Tickets/GetTickets/GetTicketsQuery.cs
//           AssetFlowCore.Domain/Enums/TicketCriticality.cs · TicketStatus.cs
//           AssetFlowCore.Domain/Repositories/TicketSearchCriteria.cs (TicketSortField)
// Resynchroniser avec : /sync-api-dtos AssetFlowCore.WebApi/Controllers/TicketsController.cs

/** Criticité d'un incident. Valeurs sérialisées en `PascalCase`. */
export type TicketCriticality = 'Low' | 'Medium' | 'High';

/** État d'un incident. */
export type TicketStatus = 'Opened' | 'InProgress' | 'Closed';

/** Champ de tri accepté par la recherche d'incidents. */
export type TicketSortField = 'CreatedAt' | 'Criticality' | 'Status' | 'Title';

/** Valeurs de `TicketCriticality` dans l'ordre du C#, de la plus faible à la plus forte. */
export const TICKET_CRITICALITIES: readonly TicketCriticality[] = ['Low', 'Medium', 'High'];

/** Valeurs de `TicketStatus` dans l'ordre du C#. */
export const TICKET_STATUSES: readonly TicketStatus[] = ['Opened', 'InProgress', 'Closed'];

/**
 * `TicketTransferHistoryDto` — entrée de l'historique de transferts (décision 0.5).
 */
export interface TicketTransferHistory {
  readonly fromTeamId: string;
  readonly fromTeamName: string;
  readonly toTeamId: string;
  readonly toTeamName: string;
  readonly reason: string;
  readonly transferredAt: string;
}

/** `TicketResponseDto` — représentation complète d'un incident de maintenance. */
export interface TicketResponse {
  readonly id: string;
  readonly assetId: string;
  readonly title: string;
  /** Description de l'anomalie, saisie à l'ouverture — jamais réécrite par un transfert. */
  readonly description: string;
  readonly criticality: TicketCriticality;
  readonly status: TicketStatus;
  /** `Guid?` côté C# : la propriété est présente et vaut `null` le cas échéant. */
  readonly assignedTeamId: string | null;
  /** Vide lorsque l'équipe n'a pas été chargée avec l'incident. */
  readonly assignedTeamName: string;
  /** Compte rendu saisi à la clôture ; `null` tant que l'incident n'est pas clôturé. */
  readonly resolutionComment: string | null;
  /** Date d'ouverture, ISO 8601. */
  readonly createdAt: string;
  /** Note d'assistance au format Markdown ; `null` tant que l'analyse IA n'a pas abouti. */
  readonly assistanceNote: string | null;
  /** Vrai tant que l'analyse IA est en cours ; repasse à faux qu'elle réussisse ou échoue. */
  readonly isAiProcessing: boolean;
  /** Auteur de la prise en charge (décision 0.2) ; `null` tant que jamais pris en charge. */
  readonly assignedByUserId: string | null;
  /** Auteur de la clôture (décision 0.2) ; `null` tant que non clôturé. */
  readonly closedByUserId: string | null;
  /**
   * Historique des transferts, du plus ancien au plus récent. Peuplé uniquement par
   * `GET /api/v1/tickets/{id}` — vide sur les autres réponses portant ce type (liste, création).
   */
  readonly transferHistory: readonly TicketTransferHistory[];
}

/**
 * `CreateTicketRequest` — corps de l'ouverture d'un incident.
 *
 * Contraintes du backend : `assetId` obligatoire (`[JsonRequired]`), titre non vide et
 * d'au plus 150 caractères, description non vide, criticité parmi `TicketCriticality`.
 * L'équipe n'est pas choisie par le client : le moteur d'assignation la déduit du couple
 * (type d'actif × criticité).
 */
export interface CreateTicketRequest {
  readonly assetId: string;
  readonly title: string;
  readonly description: string;
  readonly criticality: TicketCriticality;
}

/**
 * `CloseTicketRequest` — corps de la clôture.
 * Le compte rendu est obligatoire : vide, il produit un 400.
 */
export interface CloseTicketRequest {
  readonly resolutionComment: string;
}

/**
 * `TransferTicketRequest` — corps du transfert.
 *
 * ⚠️ `targetTeam` est le **nom** de l'équipe, pas son identifiant : le backend résout
 * l'équipe par son nom. Le motif est historisé à part (décision 0.5) et n'altère plus la
 * description de l'incident.
 */
export interface TransferTicketRequest {
  readonly targetTeam: string;
  readonly reason: string;
}

/**
 * Paramètres de `GET /api/v1/tickets`. Tous facultatifs et cumulatifs ; la casse des valeurs
 * d'énumération est indifférente côté backend.
 */
export interface TicketSearchParams {
  readonly status?: TicketStatus;
  readonly criticality?: TicketCriticality;
  readonly teamId?: string;
  readonly assetId?: string;
  /** Défaut backend : `CreatedAt`. */
  readonly sortBy?: TicketSortField;
  /** Défaut backend : `true` — les incidents les plus récents d'abord. */
  readonly sortDescending?: boolean;
  /** À partir de 1. Défaut backend : 1. */
  readonly page?: number;
  /** Défaut backend : 20, borne haute 100 (`TICKET_MAX_PAGE_SIZE`). */
  readonly pageSize?: number;
}

/**
 * Borne haute de la taille de page imposée par `GetTicketsQueryValidator.MaxPageSize`.
 * Au-delà, l'API répond 400.
 */
export const TICKET_MAX_PAGE_SIZE = 100;
