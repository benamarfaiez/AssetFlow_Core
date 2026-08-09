import { Injectable, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { AssetsApiService } from '../../../core/api/assets-api.service';
import { ApiError } from '../../../shared/models/api-error.model';
import { AssetResponse } from '../../../shared/models/asset.model';

/**
 * État de l'inventaire (E-01), **partagé avec le formulaire d'enregistrement (E-02)**.
 *
 * Fourni au niveau de la **route parente** `assets` (`Route.providers` dans `assets.routes.ts`),
 * pas en `providedIn: 'root'` : E-01 et E-02 sont deux routes sœurs sans ancêtre commun dans le
 * lot de la feature, mais elles partagent la route parente `assets`, dont l'injecteur
 * d'environnement survit à la navigation entre les deux tout en étant **détruit** dès qu'on quitte
 * `/assets/**` — l'inventaire se revalidera donc à la prochaine visite, plutôt que de rester figé
 * pour le reste de la session (défaut réel d'un choix `providedIn: 'root'`, écarté en relecture).
 * Un provider de composant, lui, ne suffirait pas : E-01 et E-02 n'ont aucun ancêtre commun *sous*
 * la route parente. C'est ce partage qui permet à `Formulaire` d'honorer le critère d'acceptation
 * explicite de P-01 : après une création, l'actif apparaît **depuis le corps de la réponse 201**,
 * jamais via un nouvel appel `GET` (voir `ajouterActif`).
 *
 * Les appels HTTP passent par `AssetsApiService`, jamais par `HttpClient` directement.
 * `rxResource` fournit `value`, `isLoading` et `error` sans état tenu à la main. Le filtrage/tri
 * local à l'écran vit dans `Inventaire` lui-même, pas ici : un état d'écran n'a pas sa place dans
 * l'état de la feature.
 *
 * Effet de bord assumé : `rxResource` sans `params` déclenche son `stream` une fois **dès la
 * construction du service**, indépendamment de toute lecture de `actifs()`. Injecter ce service
 * — y compris depuis `Formulaire`, qui ne s'en sert que pour `ajouterActif` — déclenche donc un
 * `GET /api/v1/assets` dès l'entrée sur `/assets/**`, même via `/assets/nouveau` en premier.
 * Comportement neutre à bénéfique (l'inventaire est déjà chaud au retour sur `/assets`) : à garder
 * à l'esprit dans un test qui monte `Formulaire` ou `Inventaire`, qui doit alors fournir ce
 * service explicitement (la route parente n'existe pas dans un test montant un composant seul) et
 * répondre à cette requête incidente.
 */
@Injectable()
export class InventaireService {
  private readonly api = inject(AssetsApiService);

  private readonly ressource = rxResource<readonly AssetResponse[], void>({
    stream: () => this.api.getAll(),
    defaultValue: [],
  });

  /** Inventaire renvoyé par l'API ; tableau vide tant qu'il n'a pas été chargé. */
  readonly actifs = this.ressource.value;

  /** Vrai pendant le chargement initial comme pendant un rechargement. */
  readonly chargement = this.ressource.isLoading;

  /** Erreur du dernier appel, normalisée en `ApiError` ; `null` en l'absence d'échec. */
  readonly erreur = computed(() => {
    const erreur = this.ressource.error();
    return erreur instanceof ApiError ? erreur : null;
  });

  /** Relance l'appel à l'API (bouton « Réessayer » de l'état d'erreur). */
  recharger(): void {
    this.ressource.reload();
  }

  /**
   * Ajoute un actif fraîchement créé à la liste affichée, **sans rechargement** — critère
   * d'acceptation explicite de P-01 : la liste reflète le corps de la réponse `201`, jamais un
   * nouvel appel `GET` qui pourrait servir une donnée périmée pendant la fenêtre de cache de 5
   * minutes. À appeler par le formulaire (E-02) après un succès d'enregistrement.
   */
  ajouterActif(actif: AssetResponse): void {
    this.ressource.update((liste) => [...liste, actif]);
  }
}
