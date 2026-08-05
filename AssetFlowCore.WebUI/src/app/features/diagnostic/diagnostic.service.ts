import { Injectable, computed, inject, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AssetsApiService } from '../../core/api/assets-api.service';
import { TicketHubService } from '../../core/realtime/ticket-hub.service';
import { ApiError } from '../../shared/models/api-error.model';
import { AssetResponse } from '../../shared/models/asset.model';
import { TicketResponse } from '../../shared/models/ticket.model';

/**
 * État de l'écran de diagnostic du socle.
 *
 * Les appels HTTP passent par les services de `core/api/`, jamais par `HttpClient` directement.
 * Le chargement s'appuie sur `rxResource`, qui fournit `value`, `isLoading` et `error` sans
 * état tenu à la main.
 */
@Injectable()
export class DiagnosticService {
  private readonly assetsApi = inject(AssetsApiService);
  private readonly hub = inject(TicketHubService);

  private readonly inventaire = rxResource<readonly AssetResponse[], void>({
    stream: () => this.assetsApi.getAll(),
    defaultValue: [],
  });

  private readonly _dernierIncident = signal<TicketResponse | null>(null);
  private readonly _erreurHub = signal<string | null>(null);

  /** Inventaire renvoyé par l'API ; tableau vide tant qu'il n'a pas été chargé. */
  readonly actifs = this.inventaire.value;

  /** Vrai pendant le chargement initial comme pendant un rechargement. */
  readonly chargement = this.inventaire.isLoading;

  /**
   * Erreur du dernier appel, déjà normalisée par `errorInterceptor`. Le filtrage sur `ApiError`
   * couvre le cas résiduel d'une erreur levée hors de la chaîne HTTP.
   */
  readonly erreur = computed(() => {
    const erreur = this.inventaire.error();
    if (erreur === undefined) {
      return null;
    }
    return erreur instanceof ApiError
      ? erreur
      : new ApiError({
          kind: 'server',
          status: 0,
          title: 'Erreur inattendue',
          message: erreur.message,
        });
  });

  readonly nombreActifs = computed(() => this.actifs().length);

  /** État de la liaison temps réel. */
  readonly statutTempsReel = this.hub.status;

  /** Dernier incident reçu du hub, `null` tant qu'aucun n'est arrivé. */
  readonly dernierIncident = this._dernierIncident.asReadonly();

  /** Message d'échec de la connexion au hub, `null` si aucune tentative n'a échoué. */
  readonly erreurHub = this._erreurHub.asReadonly();

  constructor() {
    // Effet de bord réel (souscription à un flux externe), et non une dérivation d'état :
    // `effect()` serait ici un contresens. `takeUntilDestroyed()` ferme l'abonnement avec le
    // service, le hub survivant à l'écran.
    this.hub.newTicket$
      .pipe(takeUntilDestroyed())
      .subscribe((incident) => this._dernierIncident.set(incident));
  }

  /** Relance l'appel à l'API. */
  recharger(): void {
    this.inventaire.reload();
  }

  /** Ouvre la liaison temps réel. L'échec est présenté, jamais avalé. */
  async connecterTempsReel(): Promise<void> {
    this._erreurHub.set(null);

    try {
      await this.hub.connect();
    } catch {
      this._erreurHub.set(
        "La liaison temps réel n'a pas pu être établie. Vérifiez que l'API est démarrée.",
      );
    }
  }
}
