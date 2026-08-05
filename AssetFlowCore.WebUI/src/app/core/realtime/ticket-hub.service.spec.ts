import { TestBed } from '@angular/core/testing';
import { HubConnection } from '@microsoft/signalr';
import { vi } from 'vitest';
import { TicketResponse } from '../../shared/models/ticket.model';
import { HUB_CONNECTION_FACTORY, TicketHubService } from './ticket-hub.service';

const INCIDENT = {
  id: '22222222-2222-2222-2222-222222222222',
  title: 'Disque saturé',
  criticality: 'High',
  assignedTeamName: 'Équipe Serveurs Critiques',
} as unknown as TicketResponse;

/**
 * Double de `HubConnection` : elle enregistre les rappels du service et permet de simuler la
 * réception d'un message, une reconnexion ou une fermeture, sans ouvrir de socket.
 */
class ConnexionSimulee {
  readonly invocations: { methode: string; argument: unknown }[] = [];
  echecAuDemarrage = false;

  private readonly ecouteurs = new Map<string, (...args: unknown[]) => void>();
  private surReconnexionEnCours?: () => void;
  private surReconnexion?: () => void;
  private surFermeture?: () => void;

  start = vi.fn(async (): Promise<void> => {
    if (this.echecAuDemarrage) {
      throw new Error('Hub injoignable');
    }
  });

  stop = vi.fn(async (): Promise<void> => this.surFermeture?.());

  invoke = vi.fn(async (methode: string, argument: unknown): Promise<void> => {
    this.invocations.push({ methode, argument });
  });

  on(nom: string, rappel: (...args: unknown[]) => void): void {
    this.ecouteurs.set(nom, rappel);
  }

  onreconnecting(rappel: () => void): void {
    this.surReconnexionEnCours = rappel;
  }

  onreconnected(rappel: () => void): void {
    this.surReconnexion = rappel;
  }

  onclose(rappel: () => void): void {
    this.surFermeture = rappel;
  }

  /** Simule un message diffusé par le serveur. */
  emettre(nom: string, charge: unknown): void {
    this.ecouteurs.get(nom)?.(charge);
  }

  simulerPerteDeConnexion(): void {
    this.surReconnexionEnCours?.();
  }

  simulerReconnexion(): void {
    this.surReconnexion?.();
  }

  /** Noms des groupes rejoints, dans l'ordre. */
  groupesRejoints(): unknown[] {
    return this.invocations
      .filter((invocation) => invocation.methode === 'JoinTeamGroup')
      .map((invocation) => invocation.argument);
  }
}

describe('TicketHubService', () => {
  let connexion: ConnexionSimulee;
  let service: TicketHubService;

  beforeEach(() => {
    connexion = new ConnexionSimulee();

    TestBed.configureTestingModule({
      providers: [
        {
          provide: HUB_CONNECTION_FACTORY,
          useValue: () => connexion as unknown as HubConnection,
        },
      ],
    });

    service = TestBed.inject(TicketHubService);
  });

  it("démarre déconnecté : rien ne se connecte au chargement de l'application", () => {
    expect(service.status()).toBe('disconnected');
    expect(service.isConnected()).toBe(false);
    expect(connexion.start).not.toHaveBeenCalled();
  });

  it('établit la liaison et la signale', async () => {
    await service.connect();

    expect(connexion.start).toHaveBeenCalledTimes(1);
    expect(service.status()).toBe('connected');
    expect(service.isConnected()).toBe(true);
  });

  it('ne duplique pas une connexion déjà établie', async () => {
    await service.connect();
    await service.connect();

    expect(connexion.start).toHaveBeenCalledTimes(1);
  });

  it('reçoit les incidents diffusés par ReceiveNewTicket', async () => {
    const recus: TicketResponse[] = [];
    service.newTicket$.subscribe((incident) => recus.push(incident));

    await service.connect();
    connexion.emettre('ReceiveNewTicket', INCIDENT);

    expect(recus).toEqual([INCIDENT]);
  });

  it("rejoint le groupe d'une équipe une fois connecté", async () => {
    await service.connect();
    await service.joinTeamGroup('Équipe Réseau');

    expect(connexion.groupesRejoints()).toEqual(['Équipe Réseau']);
  });

  it('mémorise un groupe demandé avant la connexion et le rejoint au démarrage', async () => {
    await service.joinTeamGroup('Équipe Réseau');
    expect(connexion.groupesRejoints()).toEqual([]);

    await service.connect();

    expect(connexion.groupesRejoints()).toEqual(['Équipe Réseau']);
  });

  it('restaure les groupes après une reconnexion — le serveur ne les conserve pas', async () => {
    await service.connect();
    await service.joinTeamGroup('Équipe Réseau');

    connexion.simulerPerteDeConnexion();
    expect(service.status()).toBe('reconnecting');

    connexion.simulerReconnexion();
    expect(service.status()).toBe('connected');
    expect(connexion.groupesRejoints()).toEqual(['Équipe Réseau', 'Équipe Réseau']);
  });

  it("revient à l'état déconnecté et propage l'échec du démarrage", async () => {
    connexion.echecAuDemarrage = true;

    await expect(service.connect()).rejects.toThrow('Hub injoignable');
    expect(service.status()).toBe('disconnected');
  });

  it('ferme la liaison et oublie les groupes', async () => {
    await service.connect();
    await service.joinTeamGroup('Équipe Réseau');
    await service.disconnect();

    expect(connexion.stop).toHaveBeenCalledTimes(1);
    expect(service.status()).toBe('disconnected');

    // Une reconnexion ultérieure ne réabonne pas aux groupes de la session précédente : le
    // décompte reste celui du seul abonnement demandé avant la fermeture.
    await service.connect();
    expect(connexion.groupesRejoints()).toHaveLength(1);
  });
});
