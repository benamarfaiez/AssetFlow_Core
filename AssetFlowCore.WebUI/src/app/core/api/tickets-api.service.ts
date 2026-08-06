import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/paged-result.model';
import {
  CloseTicketRequest,
  CreateTicketRequest,
  TicketResponse,
  TicketSearchParams,
  TransferTicketRequest,
} from '../../shared/models/ticket.model';

/**
 * Accès à la ressource « tickets » de l'API AssetFlow Core.
 * Contrat : AssetFlowCore.WebApi/Controllers/TicketsController.cs
 *
 * Toutes les erreurs sont normalisées en `ApiError` par `errorInterceptor`.
 */
@Injectable({ providedIn: 'root' })
export class TicketsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/tickets`;

  /**
   * Liste paginée des incidents. Les filtres sont facultatifs et se cumulent.
   * `GET /api/v1/tickets` → 200 `PagedResult<TicketResponse>`
   *
   * Erreurs : 400 (valeur d'état, de criticité ou de champ de tri hors énumération, page < 1,
   * taille de page hors 1–100), 500.
   *
   * Les paramètres absents ne sont pas transmis : le backend applique alors ses valeurs par
   * défaut (`sortBy=CreatedAt`, `sortDescending=true`, `page=1`, `pageSize=20`).
   */
  search(params: TicketSearchParams = {}): Observable<PagedResult<TicketResponse>> {
    let httpParams = new HttpParams();

    if (params.status !== undefined) httpParams = httpParams.set('status', params.status);
    if (params.criticality !== undefined) {
      httpParams = httpParams.set('criticality', params.criticality);
    }
    if (params.teamId !== undefined) httpParams = httpParams.set('teamId', params.teamId);
    if (params.assetId !== undefined) httpParams = httpParams.set('assetId', params.assetId);
    if (params.sortBy !== undefined) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDescending !== undefined) {
      httpParams = httpParams.set('sortDescending', params.sortDescending);
    }
    if (params.page !== undefined) httpParams = httpParams.set('page', params.page);
    if (params.pageSize !== undefined) httpParams = httpParams.set('pageSize', params.pageSize);

    return this.http.get<PagedResult<TicketResponse>>(this.baseUrl, { params: httpParams });
  }

  /**
   * Fiche d'un incident.
   * `GET /api/v1/tickets/{id}` → 200 `TicketResponse`
   *
   * Erreurs : 404 (incident inexistant), 500.
   */
  getById(id: string): Observable<TicketResponse> {
    return this.http.get<TicketResponse>(`${this.baseUrl}/${id}`);
  }

  /**
   * Ouvre un incident. L'équipe n'est pas choisie par le client : le moteur d'assignation la
   * déduit du couple (type d'actif × criticité) et la réponse indique celle retenue.
   * `POST /api/v1/tickets` → **201** `TicketResponse`, en-tête `Location` vers la fiche créée
   *
   * Erreurs : 400 (actif inexistant ou au rebut, titre vide ou de plus de 150 caractères,
   * description vide, criticité hors énumération, **aucune équipe ne couvre le couple
   * type × criticité** — anomalie de configuration du référentiel, non de la saisie), 500.
   *
   * `isAiProcessing` vaut `true` dans la réponse : l'analyse d'assistance est mise en file
   * après la persistance. Sa fin n'est pas notifiée aujourd'hui (Lot 6).
   */
  create(request: CreateTicketRequest): Observable<TicketResponse> {
    return this.http.post<TicketResponse>(this.baseUrl, request);
  }

  /**
   * Prend en charge un incident ouvert (passage à `InProgress`). Aucun technicien nominatif
   * n'est transmis : la prise en charge est collective (décision 0.2 non tranchée).
   * `PUT /api/v1/tickets/{id}/assign` → 204
   *
   * Erreurs : 404 (incident inexistant), 400 (l'incident n'est pas à l'état `Opened`),
   * 409 (l'incident a été modifié entre-temps), 500.
   */
  assign(id: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/assign`, null);
  }

  /**
   * Clôture un incident en cours et remet l'actif en service.
   * `PUT /api/v1/tickets/{id}/close` → 204
   *
   * Erreurs : 404 (incident inexistant), 400 (l'incident n'est pas à l'état `InProgress`,
   * compte rendu vide), 409 (l'incident a été modifié entre-temps), 500.
   */
  close(id: string, request: CloseTicketRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/close`, request);
  }

  /**
   * Transfère un incident vers une autre équipe, désignée par son **nom**.
   * `POST /api/v1/tickets/{id}/transfer` → 204
   *
   * Erreurs : 404 (incident inexistant), 400 (équipe cible introuvable ou déjà assignée,
   * incident clôturé, nom d'équipe vide), 409 (l'incident a été modifié entre-temps), 500.
   *
   * Le motif est historisé à part (décision 0.5) ; la description de l'incident n'est plus
   * modifiée par un transfert.
   */
  transfer(id: string, request: TransferTicketRequest): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/transfer`, request);
  }
}
