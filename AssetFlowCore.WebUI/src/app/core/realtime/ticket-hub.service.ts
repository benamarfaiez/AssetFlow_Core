import { Injectable, InjectionToken, computed, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TicketResponse } from '../../shared/models/ticket.model';
import { EntraAuthService } from '../auth/entra-auth.service';

/** État de la liaison temps réel, destiné à être affiché : une coupure doit être visible. */
export type RealtimeStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

/**
 * Nom de la méthode invoquée par le serveur lors de l'ouverture d'un incident.
 * Source : AssetFlowCore.Infrastructure/Notifications/SignalRNotificationService.cs
 */
const EVENEMENT_NOUVEL_INCIDENT = 'ReceiveNewTicket';

/**
 * Nom de la méthode du hub permettant de rejoindre le groupe d'une équipe.
 * Source : AssetFlowCore.Infrastructure/Notifications/TicketHub.cs
 */
const METHODE_REJOINDRE_GROUPE = 'JoinTeamGroup';

/**
 * Fabrique de connexion au hub. Isolée derrière un jeton d'injection afin que les tests
 * substituent une double sans ouvrir de socket réel.
 *
 * `accessTokenFactory` (Lot 7, étape 7.5) : SignalR l'appelle à chaque tentative de connexion,
 * y compris lors d'une reconnexion automatique — jamais un jeton capturé une fois pour toutes.
 * `EntraAuthService` est résolu ici, dans la fabrique de l'`InjectionToken` (exécutée une seule
 * fois en contexte d'injection), et capturé par la fermeture lexicale de la fonction retournée.
 */
export const HUB_CONNECTION_FACTORY = new InjectionToken<() => HubConnection>(
  'Fabrique de connexion au hub des incidents',
  {
    providedIn: 'root',
    factory: () => {
      const authentification = inject(EntraAuthService);

      return (): HubConnection =>
        new HubConnectionBuilder()
          .withUrl(`${environment.apiBaseUrl}${environment.ticketHubUrl}`, {
            accessTokenFactory: () => authentification.obtenirJetonFrais(),
          })
          // Reconnexion automatique avec les délais par défaut (0, 2, 10 puis 30 secondes).
          .withAutomaticReconnect()
          .configureLogging(environment.production ? LogLevel.Error : LogLevel.Information)
          .build();
    },
  },
);

/**
 * Client typé du hub `/ticketHub`.
 *
 * Le serveur ne diffuse aujourd'hui qu'à **l'ouverture** d'un incident, et uniquement au
 * groupe de l'équipe assignée : les changements d'état (prise en charge, clôture, transfert)
 * et la fin d'analyse IA ne sont pas notifiés (Lot 6, étapes 6.1 et 6.4).
 *
 * Rien n'est connecté au démarrage de l'application : l'appelant décide du moment
 * (`connect()`), puis des groupes à rejoindre (`joinTeamGroup()`).
 */
@Injectable({ providedIn: 'root' })
export class TicketHubService {
  private readonly creerConnexion = inject(HUB_CONNECTION_FACTORY);

  private readonly _status = signal<RealtimeStatus>('disconnected');
  private readonly _nouvelIncident = new Subject<TicketResponse>();

  /** Groupes rejoints, conservés pour être **restaurés après une reconnexion**. */
  private readonly groupes = new Set<string>();

  private connexion: HubConnection | null = null;

  /** État courant de la liaison. */
  readonly status = this._status.asReadonly();

  /** Vrai lorsque la liaison est établie. */
  readonly isConnected = computed(() => this._status() === 'connected');

  /**
   * Incidents ouverts, diffusés par le serveur aux groupes rejoints.
   * Le flux est chaud et sans mémoire : un abonné tardif ne reçoit pas les incidents passés.
   */
  readonly newTicket$: Observable<TicketResponse> = this._nouvelIncident.asObservable();

  /**
   * Établit la liaison. Appel idempotent : une connexion déjà ouverte ou en cours n'est pas
   * dupliquée. Une erreur de démarrage est propagée à l'appelant, qui décide de réessayer.
   */
  async connect(): Promise<void> {
    if (this.connexion !== null) {
      return;
    }

    const connexion = this.creerConnexion();
    this.connexion = connexion;
    this._status.set('connecting');

    connexion.on(EVENEMENT_NOUVEL_INCIDENT, (ticket: TicketResponse) => {
      this._nouvelIncident.next(ticket);
    });

    connexion.onreconnecting(() => this._status.set('reconnecting'));

    connexion.onreconnected(() => {
      this._status.set('connected');
      // Le serveur ne conserve pas l'appartenance aux groupes d'une connexion perdue :
      // sans cette restauration, la reconnexion serait silencieusement sourde.
      void this.rejoindreGroupesConnus();
    });

    connexion.onclose(() => {
      this._status.set('disconnected');
      this.connexion = null;
    });

    try {
      await connexion.start();
      this._status.set('connected');
      await this.rejoindreGroupesConnus();
    } catch (erreur) {
      this._status.set('disconnected');
      this.connexion = null;
      throw erreur;
    }
  }

  /**
   * Rejoint le groupe temps réel d'une équipe, désignée par son **nom** (le hub attend un nom,
   * pas un identifiant). Le groupe est mémorisé : il sera rejoint à la connexion suivante et
   * restauré après une reconnexion.
   */
  async joinTeamGroup(teamName: string): Promise<void> {
    this.groupes.add(teamName);

    if (this.isConnected()) {
      await this.connexion?.invoke(METHODE_REJOINDRE_GROUPE, teamName);
    }
  }

  /** Ferme la liaison et oublie les groupes rejoints. */
  async disconnect(): Promise<void> {
    const connexion = this.connexion;
    this.groupes.clear();

    if (connexion === null) {
      return;
    }

    this.connexion = null;
    await connexion.stop();
    this._status.set('disconnected');
  }

  private async rejoindreGroupesConnus(): Promise<void> {
    for (const groupe of this.groupes) {
      await this.connexion?.invoke(METHODE_REJOINDRE_GROUPE, groupe);
    }
  }
}
