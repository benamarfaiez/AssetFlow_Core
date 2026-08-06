// Contrat synchronisé depuis le backend .NET — ne pas modifier à la main.
// Sources : AssetFlowCore.Application/DTOs/TeamResponseDto.cs
//           AssetFlowCore.WebApi/Requests/CreateTeamRequest.cs · UpdateTeamRequest.cs
// Resynchroniser avec : /sync-api-dtos AssetFlowCore.WebApi/Controllers/TeamsController.cs

import { AssetType } from './asset.model';
import { TicketCriticality } from './ticket.model';

/**
 * `TeamResponseDto` — équipe technique.
 *
 * `assetType` et `ticketCriticality` sont persistés en **texte** côté base (colonnes `string`,
 * et non les énumérations du domaine) : le moteur d'assignation compare `assetType.ToString()`.
 * Les valeurs restent donc celles des énumérations, d'où le typage en union.
 */
export interface TeamResponse {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  /** Pilotable via `TeamsApiService.activate`/`deactivate` (décision 0.6), réservé à l'administrateur. */
  readonly isActive: boolean;
  readonly createdAt: string;
  readonly assetType: AssetType;
  readonly ticketCriticality: TicketCriticality;
}

/**
 * `CreateTeamRequest` — corps de la création d'une équipe.
 *
 * Contraintes du backend : nom non vide, d'au plus 100 caractères et **unique** (contrôle
 * applicatif, doublon → 400) ; `assetType` et `ticketCriticality` obligatoires et pris dans
 * les énumérations ; description d'au plus 500 caractères.
 */
export interface CreateTeamRequest {
  readonly name: string;
  readonly assetType: AssetType;
  readonly ticketCriticality: TicketCriticality;
  readonly description?: string | null;
}

/**
 * `UpdateTeamRequest` — corps de la mise à jour. Tous les champs sont facultatifs côté C#
 * (`string?`) : seuls ceux fournis sont appliqués.
 */
export interface UpdateTeamRequest {
  readonly name?: string | null;
  readonly assetType?: AssetType | null;
  readonly ticketCriticality?: TicketCriticality | null;
  readonly description?: string | null;
}
