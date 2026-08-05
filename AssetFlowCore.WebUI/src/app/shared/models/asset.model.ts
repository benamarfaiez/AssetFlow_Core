// Contrat synchronisé depuis le backend .NET — ne pas modifier à la main.
// Sources : AssetFlowCore.Application/DTOs/AssetResponseDto.cs
//           AssetFlowCore.Application/DTOs/AssetDetailResponseDto.cs
//           AssetFlowCore.WebApi/Requests/RegisterAssetRequest.cs
//           AssetFlowCore.Domain/Enums/AssetType.cs · AssetStatus.cs
// Resynchroniser avec : /sync-api-dtos AssetFlowCore.WebApi/Controllers/AssetsController.cs

import { TicketCriticality, TicketStatus } from './ticket.model';

/**
 * Type d'actif. Les valeurs circulent en `PascalCase` : `Program.cs` enregistre
 * `JsonStringEnumConverter` **sans** politique de nommage, seuls les noms de propriétés
 * passent en camelCase.
 */
export type AssetType = 'Server' | 'Laptop' | 'NetworkDevice';

/** État d'un actif dans son cycle de vie. */
export type AssetStatus = 'InService' | 'Down' | 'InMaintenance' | 'Decommissioned';

/** Valeurs de `AssetType` dans l'ordre du C#, pour alimenter un sélecteur sans les redéclarer. */
export const ASSET_TYPES: readonly AssetType[] = ['Server', 'Laptop', 'NetworkDevice'];

/** Valeurs de `AssetStatus` dans l'ordre du C#. */
export const ASSET_STATUSES: readonly AssetStatus[] = [
  'InService',
  'Down',
  'InMaintenance',
  'Decommissioned',
];

/**
 * `AssetResponseDto` — actif tel que présenté dans l'inventaire.
 *
 * `type` et `status` sont déclarés `string` côté C# (projection de `Type.ToString()`) : les
 * valeurs restent celles des énumérations du domaine, d'où le typage en union ici.
 */
export interface AssetResponse {
  readonly id: string;
  readonly name: string;
  readonly serialNumber: string;
  readonly type: AssetType;
  readonly status: AssetStatus;
  /** `DateTime` C# sérialisé en ISO 8601 — jamais typé `Date` au niveau du transport. */
  readonly createdAt: string;
}

/**
 * `AssetTicketDto` — incident tel que présenté dans la fiche d'un actif. Le contexte de
 * l'actif étant porté par la fiche, seuls les éléments propres à l'incident sont repris.
 */
export interface AssetTicketSummary {
  readonly id: string;
  readonly title: string;
  readonly criticality: TicketCriticality;
  readonly status: TicketStatus;
  readonly createdAt: string;
  readonly assignedTeamId: string;
  /** Vide lorsque l'équipe n'a pas été chargée avec l'incident. */
  readonly assignedTeamName: string;
}

/**
 * `AssetDetailResponseDto` — fiche d'un actif, incidents inclus, du plus récent au plus ancien.
 */
export interface AssetDetailResponse extends AssetResponse {
  readonly tickets: readonly AssetTicketSummary[];
}

/**
 * `RegisterAssetRequest` — corps de la création d'un actif.
 *
 * Contraintes appliquées par le backend : nom non vide, numéro de série de 5 à 50 caractères
 * et unique dans le parc (comparaison en majuscules, sans espaces de bordure), `type` parmi
 * les valeurs de `AssetType`.
 */
export interface RegisterAssetRequest {
  readonly name: string;
  readonly serialNumber: string;
  readonly type: AssetType;
}
