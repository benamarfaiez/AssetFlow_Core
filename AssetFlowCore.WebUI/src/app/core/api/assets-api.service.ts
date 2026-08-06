import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AssetDetailResponse,
  AssetResponse,
  RegisterAssetRequest,
  RestoreAssetToServiceRequest,
} from '../../shared/models/asset.model';

/**
 * Accès à la ressource « assets » de l'API AssetFlow Core.
 * Contrat : AssetFlowCore.WebApi/Controllers/AssetsController.cs
 *
 * Toutes les erreurs sont normalisées en `ApiError` par `errorInterceptor` : les appelants
 * n'ont pas à interpréter `HttpErrorResponse`.
 */
@Injectable({ providedIn: 'root' })
export class AssetsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/v1/assets`;

  /**
   * Liste l'inventaire complet des actifs, triés par le backend.
   * `GET /api/v1/assets` → 200 `AssetResponse[]`
   *
   * Erreurs : 500. Pas de pagination : la collection est servie en intégralité.
   * La réponse provient d'un cache serveur de 5 minutes, invalidé par les écritures — un
   * rechargement après création ou mise au rebut reflète donc l'état réel.
   */
  getAll(): Observable<readonly AssetResponse[]> {
    return this.http.get<readonly AssetResponse[]>(this.baseUrl);
  }

  /**
   * Fiche d'un actif, ses incidents inclus, du plus récent au plus ancien.
   * `GET /api/v1/assets/{id}` → 200 `AssetDetailResponse`
   *
   * Erreurs : 404 (actif inexistant), 500.
   */
  getById(id: string): Observable<AssetDetailResponse> {
    return this.http.get<AssetDetailResponse>(`${this.baseUrl}/${id}`);
  }

  /**
   * Enregistre un nouvel actif ; il naît à l'état `InService`.
   * `POST /api/v1/assets` → **201** `AssetResponse`, en-tête `Location` vers la fiche créée
   *
   * Erreurs : 400 (numéro de série déjà enregistré, longueur hors 5–50 caractères, nom vide,
   * type hors énumération), 500.
   */
  register(request: RegisterAssetRequest): Observable<AssetResponse> {
    return this.http.post<AssetResponse>(this.baseUrl, request);
  }

  /**
   * Met un actif au rebut. Réversible via `restoreToService` (décision 0.4).
   * `PUT /api/v1/assets/{id}/decommission` → 204
   *
   * Erreurs : 404 (actif inexistant), 400 (l'actif porte des incidents en cours — le message
   * en précise le nombre), 500.
   */
  decommission(id: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/decommission`, null);
  }

  /**
   * Remet en service un actif mis au rebut. Motif obligatoire, réservé au rôle `Administrateur`.
   * `PUT /api/v1/assets/{id}/restore-to-service` → 204
   *
   * Erreurs : 404 (actif inexistant), 400 (motif vide, actif non `Decommissioned`),
   * 403 (rôle insuffisant), 500.
   */
  restoreToService(id: string, request: RestoreAssetToServiceRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/restore-to-service`, request);
  }
}
