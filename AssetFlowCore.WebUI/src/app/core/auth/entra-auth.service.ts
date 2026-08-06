import { InjectionToken, Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import {
  InteractionRequiredAuthError,
  PublicClientApplication,
  type AuthenticationResult,
  type IPublicClientApplication,
} from '@azure/msal-browser';
import { environment } from '../../../environments/environment';
import { AuthTokenService } from './auth-token.service';
import {
  ENTRA_CONFIG,
  creerConfigurationMsal,
  creerRequeteConnexion,
  creerRequeteSilencieuse,
} from './msal.config';

/** Marge prise avant l'expiration réelle du jeton pour déclencher son renouvellement silencieux. */
const MARGE_RENOUVELLEMENT_MS = 2 * 60 * 1000;

/**
 * Fabrique du client MSAL. Isolée derrière un jeton d'injection, à l'image de
 * `HUB_CONNECTION_FACTORY` (`core/realtime/ticket-hub.service.ts`), afin que les tests
 * substituent une double sans construire de véritable `PublicClientApplication` (crypto
 * `SubtleCrypto`, stockage de navigateur… indisponibles ou superflus sous jsdom).
 */
export const MSAL_CLIENT_FACTORY = new InjectionToken<() => IPublicClientApplication>(
  'Fabrique du client MSAL (PublicClientApplication)',
  {
    providedIn: 'root',
    factory: () => {
      const config = inject(ENTRA_CONFIG);
      return (): IPublicClientApplication =>
        new PublicClientApplication(creerConfigurationMsal(config));
    },
  },
);

/**
 * Vrai si `chemin` désigne une destination interne à l'application — un chemin absolu
 * (`/diagnostic`), jamais une URL absolue (`https://…`) ni un chemin protocole-relatif
 * (`//hote/…`), qui ouvrirait une redirection vers un site externe après connexion.
 */
export function estCheminInterne(chemin: string): boolean {
  return chemin.startsWith('/') && !chemin.startsWith('//') && !chemin.includes('\\');
}

/**
 * Orchestre le flux OIDC (Authorization Code + PKCE) porté par `@azure/msal-browser`, en
 * bibliothèque de flux uniquement (décision posée avec l'utilisateur) : ni `MsalModule`, ni
 * `MsalGuard`, ni surtout `MsalInterceptor`, qui dupliqueraient le rôle déjà tenu par
 * `authTokenInterceptor`/`AuthTokenService` et par la garde de route de ce lot.
 *
 * Cycle de vie :
 * 1. `initialiser()` — appelée une seule fois par `provideAppInitializer` avant le démarrage du
 *    routeur : traite un retour de redirection (`handleRedirectPromise`) ou, à défaut, tente une
 *    connexion silencieuse pour un compte déjà en cache (persistance du cache MSAL :
 *    `sessionStorage`, restaurée après un F5).
 * 2. `connecterEtRevenirA(destination)` — appelée par la garde de route quand l'utilisateur n'est
 *    pas authentifié : lance une redirection complète vers Entra ID, `destination` faisant l'aller-retour
 *    via l'état `state` de la requête OIDC (rien d'autre ne survit à une redirection de page entière).
 * 3. `renouvelerJeton()` / `obtenirJetonFrais()` — renouvellement silencieux, programmé avant
 *    expiration et rejoué à la demande (rejeu 401, `accessTokenFactory` de SignalR).
 *
 * Tant que le tenant Entra ID (étape 7.0) n'est pas renseigné ({@link ENTRA_CONFIG}.`clientId`
 * vide), aucune instance MSAL n'est créée : le service reste inerte, à l'image du reste de
 * l'authentification côté API.
 */
@Injectable({ providedIn: 'root' })
export class EntraAuthService {
  private readonly jetons = inject(AuthTokenService);
  private readonly router = inject(Router);
  private readonly creerClient = inject(MSAL_CLIENT_FACTORY);
  private readonly config = inject(ENTRA_CONFIG);

  /**
   * Vrai dès que les trois informations indispensables à MSAL sont renseignées — indépendant de
   * la validité réelle du tenant. Une configuration partielle (par exemple `clientId` seul) ne
   * suffit pas : `PublicClientApplication` n'aurait de toute façon pas de quoi fonctionner.
   */
  readonly estConfigure =
    this.config.clientId !== '' && this.config.authority !== '' && this.config.redirectUri !== '';

  private client: IPublicClientApplication | null = null;
  private minuteurRenouvellement: ReturnType<typeof setTimeout> | null = null;
  private connexionEnCours: Promise<void> | null = null;
  private renouvellementEnCours: Promise<string | null> | null = null;

  private resolverPret!: () => void;

  /**
   * Résolue une fois l'initialisation terminée (redirection traitée, connexion silencieuse
   * tentée). Ne rejette **jamais** : la garde de route qui l'attend ne doit jamais rester
   * bloquée par un incident d'initialisation (réseau, tenant mal configuré).
   */
  readonly pretAuthentification: Promise<void> = new Promise((resolve) => {
    this.resolverPret = resolve;
  });

  /**
   * Point d'entrée unique, appelé par `provideAppInitializer` avant que le routeur ne s'exécute.
   */
  async initialiser(): Promise<void> {
    if (!this.estConfigure) {
      this.resolverPret();
      return;
    }

    try {
      // La construction du client (`new PublicClientApplication(...)`) est à l'intérieur du
      // `try` : un échec synchrone (configuration Entra ID incompatible avec l'environnement du
      // navigateur, `SubtleCrypto` absent, stockage bloqué) ne doit pas remonter hors de cette
      // méthode, appelée par `provideAppInitializer` — un rejet à ce niveau empêcherait
      // `bootstrapApplication` de rendre le moindre composant.
      const client = this.creerClient();
      this.client = client;

      await client.initialize();
      // `navigateToLoginRequestUrl: false` : la restauration de la page d'origine après
      // connexion est prise en charge ci-dessous (état `state`, via le routeur Angular) — MSAL
      // ne doit pas reprendre lui-même la page qui a déclenché la connexion.
      const resultat = await client.handleRedirectPromise({ navigateToLoginRequestUrl: false });

      if (resultat !== null) {
        this.appliquerResultat(resultat);
        this.revenirADestination(resultat.state);
      } else {
        await this.tenterConnexionSilencieuse();
      }
    } catch {
      // Échec d'initialisation (construction du client, réseau, autorité injoignable) :
      // l'utilisateur reste anonyme et le service redevient inerte — la prochaine navigation
      // protégée redéclenchera une connexion interactive plutôt que d'appeler une méthode sur un
      // client MSAL potentiellement cassé ou non initialisé.
      this.client = null;
      this.jetons.clear();
    } finally {
      this.resolverPret();
    }
  }

  /**
   * Lance une connexion interactive (redirection complète, jamais de popup) et mémorise
   * `destination` pour y revenir une fois connecté. Idempotent : un appel concurrent à une
   * connexion déjà en cours ne relance pas `loginRedirect` (MSAL refuse toute interaction
   * simultanée).
   */
  async connecterEtRevenirA(destination: string): Promise<void> {
    if (!this.estConfigure || this.client === null) {
      if (!environment.production) {
        // Aucun jeton ni revendication dans ce message : uniquement l'état de configuration.
        console.warn(
          "[Auth] Entra ID non configuré (étape 7.0 du Lot 7) : connexion impossible pour l'instant.",
        );
      }
      return;
    }

    if (this.connexionEnCours !== null) {
      return this.connexionEnCours;
    }

    const client = this.client;
    const requete = creerRequeteConnexion(
      this.config,
      estCheminInterne(destination) ? destination : undefined,
    );

    const connexion = client.loginRedirect(requete).finally(() => {
      this.connexionEnCours = null;
    });

    this.connexionEnCours = connexion;
    return connexion;
  }

  /**
   * Renouvelle le jeton d'accès pour le compte actif, silencieusement. Utilisée à la fois par le
   * renouvellement programmé et par le rejeu d'un 401 (`sessionRenewalInterceptor`).
   *
   * @returns Le nouveau jeton, ou `null` si aucun compte n'est actif ou si le renouvellement
   * silencieux échoue — dans ce dernier cas, le jeton en mémoire est oublié : la prochaine
   * navigation protégée redéclenchera une connexion interactive plutôt que d'en tenter une
   * depuis ce contexte non interactif (intercepteur, minuteur).
   */
  async renouvelerJeton(): Promise<string | null> {
    if (this.renouvellementEnCours !== null) {
      return this.renouvellementEnCours;
    }

    const renouvellement = this.effectuerRenouvellement().finally(() => {
      this.renouvellementEnCours = null;
    });

    this.renouvellementEnCours = renouvellement;
    return renouvellement;
  }

  /**
   * Mutualise les appels concurrents à `acquireTokenSilent` (même schéma que
   * `connecterEtRevenirA`/`connexionEnCours`) : si dix requêtes API échouent en 401
   * simultanément, `sessionRenewalInterceptor` appelle `renouvelerJeton()` une fois par requête —
   * un seul appel silencieux doit partir vers Entra ID, faute de quoi la rotation de jetons
   * risquerait de s'invalider mutuellement.
   */
  private async effectuerRenouvellement(): Promise<string | null> {
    const client = this.client;
    const compte = client?.getActiveAccount() ?? null;

    if (client === null || compte === null) {
      return null;
    }

    try {
      const resultat = await client.acquireTokenSilent(
        creerRequeteSilencieuse(this.config, compte),
      );
      this.appliquerResultat(resultat);
      return resultat.accessToken;
    } catch (erreur) {
      this.jetons.clear();

      if (!environment.production && erreur instanceof InteractionRequiredAuthError) {
        console.warn(
          '[Auth] Renouvellement silencieux impossible : une connexion interactive sera nécessaire.',
        );
      }

      return null;
    }
  }

  /**
   * Jeton frais pour `accessTokenFactory` (`TicketHubService`) : jamais une valeur capturée à la
   * construction de la connexion — `acquireTokenSilent` est réinterrogé à chaque appel, donc à
   * chaque tentative de connexion et de reconnexion automatique de SignalR.
   */
  async obtenirJetonFrais(): Promise<string> {
    const jeton = await this.renouvelerJeton();
    return jeton ?? '';
  }

  private async tenterConnexionSilencieuse(): Promise<void> {
    const client = this.client;

    if (client === null) {
      return;
    }

    const compte = client.getAllAccounts()[0];

    if (compte === undefined) {
      return;
    }

    client.setActiveAccount(compte);

    try {
      const resultat = await client.acquireTokenSilent(
        creerRequeteSilencieuse(this.config, compte),
      );
      this.appliquerResultat(resultat);
    } catch {
      // Session expirée ou révoquée depuis la dernière visite : reste anonyme jusqu'à la
      // prochaine connexion interactive.
      this.jetons.clear();
    }
  }

  private appliquerResultat(resultat: AuthenticationResult): void {
    if (resultat.account !== null) {
      this.client?.setActiveAccount(resultat.account);
    }

    this.jetons.store(resultat.accessToken);
    this.programmerRenouvellement(resultat.expiresOn);
  }

  private programmerRenouvellement(expiration: Date | null): void {
    if (this.minuteurRenouvellement !== null) {
      clearTimeout(this.minuteurRenouvellement);
      this.minuteurRenouvellement = null;
    }

    if (expiration === null) {
      return;
    }

    const delai = Math.max(expiration.getTime() - Date.now() - MARGE_RENOUVELLEMENT_MS, 0);

    this.minuteurRenouvellement = setTimeout(() => {
      void this.renouvelerJeton();
    }, delai);
  }

  /** Restaure la page demandée avant la connexion, si elle a survécu (état `state` de MSAL). */
  private revenirADestination(destination: string | undefined): void {
    if (destination === undefined || !estCheminInterne(destination)) {
      return;
    }

    void this.router.navigateByUrl(destination);
  }
}
