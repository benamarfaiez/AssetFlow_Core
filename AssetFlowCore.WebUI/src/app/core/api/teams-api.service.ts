import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateTeamRequest, TeamResponse, UpdateTeamRequest } from '../../shared/models/team.model';

/**
 * Accès à la ressource « teams » de l'API AssetFlow Core.
 * Contrat : AssetFlowCore.WebApi/Controllers/TeamsController.cs
 *
 * Toutes les erreurs sont normalisées en `ApiError` par `errorInterceptor`.
 */
@Injectable({ providedIn: 'root' })
export class TeamsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/teams`;

  /**
   * Liste des équipes, triée par nom par le backend.
   * `GET /api/v1/teams` → 200 `TeamResponse[]`
   *
   * Erreurs : 500. Pas de pagination : la collection est servie en intégralité.
   *
   * @param onlyActive Vrai pour ne retenir que les équipes actives — la forme attendue par un
   * sélecteur d'équipe cible. L'écran d'administration passe `false` afin de voir aussi les
   * équipes désactivées.
   */
  getAll(onlyActive = false): Observable<readonly TeamResponse[]> {
    return this.http.get<readonly TeamResponse[]>(this.baseUrl, { params: { onlyActive } });
  }

  /**
   * Fiche d'une équipe.
   * `GET /api/v1/teams/{id}` → 200 `TeamResponse`
   *
   * Erreurs : 404 (équipe inexistante), 500.
   */
  getById(id: string): Observable<TeamResponse> {
    return this.http.get<TeamResponse>(`${this.baseUrl}/${id}`);
  }

  /**
   * Crée une équipe. Le couple (type d'actif × criticité) détermine les incidents qu'elle
   * recevra : une combinaison non couverte fait échouer l'ouverture d'incident correspondante.
   * `POST /api/v1/teams` → **201** `TeamResponse`, en-tête `Location` vers la fiche créée
   *
   * Erreurs : 400 (nom vide, de plus de 100 caractères ou **déjà pris**, type d'actif ou
   * criticité hors énumération, description de plus de 500 caractères), 500.
   */
  create(request: CreateTeamRequest): Observable<TeamResponse> {
    return this.http.post<TeamResponse>(this.baseUrl, request);
  }

  /**
   * Met à jour une équipe. Tous les champs sont facultatifs : seuls ceux fournis sont appliqués.
   * `PUT /api/v1/teams/{id}` → **200** `TeamResponse`
   *
   * Erreurs : 404 (équipe inexistante), 400 (nom déjà porté par une autre équipe, longueurs
   * dépassées, valeurs hors énumération), 500.
   */
  update(id: string, request: UpdateTeamRequest): Observable<TeamResponse> {
    return this.http.put<TeamResponse>(`${this.baseUrl}/${id}`, request);
  }

  /**
   * Supprime une équipe.
   * `DELETE /api/v1/teams/{id}` → 204
   *
   * Erreurs : 404 (équipe inexistante), 400 (des incidents actifs lui sont assignés), 500.
   *
   * La suppression reste définitive ; l'activation/désactivation (décision 0.6) offre une
   * alternative réversible pour les équipes créées par erreur.
   */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /**
   * Réactive une équipe désactivée. Réservé au rôle `Administrateur`.
   * `PUT /api/v1/teams/{id}/activate` → 200 `TeamResponse`
   *
   * Erreurs : 404 (équipe inexistante), 403 (rôle insuffisant), 500.
   */
  activate(id: string): Observable<TeamResponse> {
    return this.http.put<TeamResponse>(`${this.baseUrl}/${id}/activate`, null);
  }

  /**
   * Désactive une équipe : elle disparaît de `?onlyActive=true` et cesse de recevoir de
   * nouveaux incidents, sans être supprimée. Réservé au rôle `Administrateur`.
   * `PUT /api/v1/teams/{id}/deactivate` → 200 `TeamResponse`
   *
   * Erreurs : 404 (équipe inexistante), 403 (rôle insuffisant), 500.
   */
  deactivate(id: string): Observable<TeamResponse> {
    return this.http.put<TeamResponse>(`${this.baseUrl}/${id}/deactivate`, null);
  }
}
