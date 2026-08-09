import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  Signal,
  TemplateRef,
  afterNextRender,
  computed,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, ValidationErrors } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { JwtRolesService } from '../../../core/auth/jwt-roles.service';
import { AssetsApiService } from '../../../core/api/assets-api.service';
import { focusPremierChampInvalide } from '../../../shared/forms/focus-invalide';
import {
  LIBELLES_TICKET_CRITICALITY,
  LIBELLES_TICKET_STATUS,
  traduire,
} from '../../../shared/i18n/libelles';
import { ApiError } from '../../../shared/models/api-error.model';
import {
  AssetTicketSummary,
  RestoreAssetToServiceRequest,
} from '../../../shared/models/asset.model';
import { AssetTypeLabelPipe } from '../../../shared/pipes/libelles.pipe';
import { Button } from '../../../shared/ui/button/button';
import { Card } from '../../../shared/ui/card/card';
import { ConfirmDialog } from '../../../shared/ui/confirm-dialog/confirm-dialog';
import { ColonneTable, DataTable } from '../../../shared/ui/data-table/data-table';
import { ErrorMessage } from '../../../shared/ui/error-message/error-message';
import { EmptyState } from '../../../shared/ui/empty-state/empty-state';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import {
  AssetStatusBadge,
  TicketCriticalityBadge,
  TicketStatusBadge,
} from '../../../shared/ui/status-badges/status-badges';
import { TextareaField } from '../../../shared/ui/textarea-field/textarea-field';

/**
 * Aucun pipe de date partagé n'existe encore dans `shared/` (le premier écran à en avoir besoin
 * est celui-ci). Un formateur `Intl` local et déterministe (`timeZone: 'UTC'`, pour ne pas
 * dépendre du fuseau de la machine qui exécute les tests) suffit ici : il sert à la fois à la
 * date de création de l'en-tête et à la colonne « Date » du tableau d'incidents, où
 * `ColonneTable.valeur` attend une chaîne déjà formatée plutôt qu'un gabarit. Un `| date` natif
 * (`LOCALE_ID` déjà fixé à `fr-FR` dans `app.config.ts`) aurait tout aussi bien convenu pour la
 * seule ligne d'en-tête ; ce choix couvre les deux emplacements avec un mécanisme unique.
 */
const FORMATEUR_DATE = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'medium', timeZone: 'UTC' });

function formaterDate(dateIso: string): string {
  return FORMATEUR_DATE.format(new Date(dateIso));
}

/**
 * Seule vérification réellement appliquée côté API pour la remise en service (motif requis par
 * l'entité du domaine, sans validateur FluentValidation dédié — bug connu, hors périmètre de
 * correction) : non vide après suppression des espaces de bordure. C'est donc la seule protection
 * réelle contre un aller-retour réseau inutile — voir `confirmerRemiseEnService` ci-dessous.
 */
function validerMotifNonVide(controle: AbstractControl<string>): ValidationErrors | null {
  return controle.value.trim().length === 0 ? { motifVide: true } : null;
}

/**
 * E-03 — Fiche d'un actif.
 *
 * `id` est alimenté directement par le paramètre de route (`withComponentInputBinding()`,
 * `app.config.ts`) : pas de lecture manuelle d'`ActivatedRoute`. `params` (et non un `stream`
 * fermé sur `id()`) est ce qui rend `rxResource` réactif à un changement d'identifiant — cf.
 * `ResourceLoaderParams`, `@angular/core` 22.1.0.
 *
 * La ressource reste au niveau du composant, à la différence d'`InventaireService` : elle est
 * mécaniquement liée à l'entrée de route, sans logique réutilisable à en extraire pour l'instant.
 * `InventaireService` est techniquement injectable ici aussi (même route parente `assets`, voir
 * `assets.routes.ts`), mais volontairement pas utilisé : une mise au rebut ou une remise en
 * service ne met à jour que **cette** fiche (`recharger()`) ; l'inventaire, lui, se revalidera de
 * lui-même à la prochaine visite de `/assets` (son cache serveur de 5 minutes est de toute façon
 * invalidé côté API par ces deux écritures). Synchroniser les deux écrans en temps réel serait une
 * complexité que rien n'exige ici.
 *
 * Actions couvertes ici : « Ouvrir un incident » (P-02, vers `/tickets/nouveau?assetId=...`,
 * indisponible sur un actif déjà `Decommissioned`, RM-09), « Mettre au rebut » (P-06, RM-06,
 * RM-07 — tout utilisateur authentifié, hors actif déjà `Decommissioned`) et « Remettre en
 * service » (P-06 bis, RM-28, décision 0.4 — réservée au rôle `Administrateur`, motif obligatoire).
 * Aucun lien vers une fiche d'incident (E-05) depuis le tableau ci-dessous : la feature `tickets`
 * pointera plus tard vers cette fiche via `assetId`, la réciproque (fiche actif → fiche incident)
 * n'a pas été demandée et ajouterait une dépendance qu'aucun ancêtre de route ne justifie.
 */
@Component({
  selector: 'app-assets-fiche',
  imports: [
    AssetStatusBadge,
    AssetTypeLabelPipe,
    Button,
    Card,
    ConfirmDialog,
    DataTable,
    EmptyState,
    ErrorMessage,
    RouterLink,
    Spinner,
    TextareaField,
    TicketCriticalityBadge,
    TicketStatusBadge,
  ],
  templateUrl: './fiche.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Fiche {
  private readonly api = inject(AssetsApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /**
   * Ergonomie uniquement : masque le bouton pour un rôle absent, mais l'API tranche réellement
   * (403 sinon) — voir l'avertissement de tête de `JwtRolesService`. Le masquage n'est donc jamais
   * la seule protection ; `messageErreurRemiseEnService` gère proprement le cas résiduel d'un 403.
   */
  private readonly jwtRoles = inject(JwtRolesService);

  readonly id = input.required<string>();

  private readonly ressource = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.api.getById(params),
  });

  /** `undefined` tant que la fiche n'a pas été chargée avec succès. */
  protected readonly actif = this.ressource.value;

  protected readonly chargement = this.ressource.isLoading;

  /** Erreur du dernier appel (dont 404 : « Cet équipement n'existe plus. »), `null` sinon. */
  protected readonly erreur = computed(() => {
    const erreur = this.ressource.error();
    return erreur instanceof ApiError ? erreur : null;
  });

  /**
   * Message à afficher pour l'échec du chargement initial. Ne relaie **jamais** `erreur.message`
   * tel quel sur un 404 : `errorInterceptor` préfère le `detail` du backend quand il existe
   * (`message: detail || MESSAGES.notFound`), et ce `detail` contient ici littéralement le GUID
   * de l'actif (« L'actif {id} est introuvable. ») — règle transverse enfreinte sinon (« ne jamais
   * afficher un identifiant technique dans un message destiné à l'utilisateur »). Prend l'erreur
   * déjà réduite au non-nul par le gabarit (`@else if (erreur(); as erreurFiche)`) plutôt qu'un
   * second `computed()` sur `erreur()`, pour ne pas avoir à réaffirmer sa nullité.
   */
  protected messageErreurChargement(erreurCourante: ApiError): string {
    return erreurCourante.kind === 'notFound'
      ? this.messageEquipementIntrouvable
      : erreurCourante.message;
  }

  protected recharger(): void {
    this.ressource.reload();
  }

  // --- Textes localisés destinés aux `input()` de composants enfants -------------------------
  // (texte de gabarit HTML : simple attribut `i18n` sur l'élément — voir `fiche.html`.)

  protected readonly libelleChargement = $localize`:@@assets.fiche.chargement:Chargement de la fiche…`;
  protected readonly titreErreurChargement = $localize`:@@assets.fiche.erreurChargement.titre:Chargement impossible`;
  protected readonly messageActifIntrouvable = $localize`:@@assets.fiche.introuvable:Cet actif est introuvable.`;

  protected readonly legendeIncidents = $localize`:@@assets.fiche.incidents.legende:Incidents de cet actif`;
  protected readonly messageAucunIncident = $localize`:@@assets.fiche.incidents.aucun:Aucun incident enregistré pour cet actif.`;
  private readonly libelleColonneTitre = $localize`:@@assets.fiche.incidents.colonneTitre:Titre`;
  private readonly libelleColonneCriticite = $localize`:@@assets.fiche.incidents.colonneCriticite:Criticité`;
  private readonly libelleColonneStatut = $localize`:@@assets.fiche.incidents.colonneStatut:Statut`;
  private readonly libelleColonneDate = $localize`:@@assets.fiche.incidents.colonneDate:Date d'ouverture`;

  private readonly messageEquipementIntrouvable = $localize`:@@assets.fiche.equipementIntrouvable:Cet équipement n'existe plus. Actualisez la liste.`;
  private readonly messageErreurInattendue = $localize`:@@assets.fiche.erreurInattendue:Une erreur inattendue est survenue.`;

  // --- Section incidents -----------------------------------------------------------------------

  private readonly gabaritCriticite =
    viewChild.required<TemplateRef<{ $implicit: AssetTicketSummary }>>('gabaritCriticite');
  private readonly gabaritStatutTicket =
    viewChild.required<TemplateRef<{ $implicit: AssetTicketSummary }>>('gabaritStatutTicket');

  protected readonly colonnesIncidents = computed<readonly ColonneTable<AssetTicketSummary>[]>(
    () => [
      { cle: 'titre', entete: this.libelleColonneTitre, valeur: (ticket) => ticket.title },
      {
        cle: 'criticite',
        entete: this.libelleColonneCriticite,
        valeur: (ticket) => traduire(LIBELLES_TICKET_CRITICALITY, ticket.criticality),
        gabarit: this.gabaritCriticite(),
      },
      {
        cle: 'statut',
        entete: this.libelleColonneStatut,
        valeur: (ticket) => traduire(LIBELLES_TICKET_STATUS, ticket.status),
        gabarit: this.gabaritStatutTicket(),
      },
      {
        cle: 'date',
        entete: this.libelleColonneDate,
        valeur: (ticket) => formaterDate(ticket.createdAt),
      },
    ],
  );

  protected readonly cleTicket = (ticket: AssetTicketSummary): string => ticket.id;

  protected dateCreation(dateIso: string): string {
    return formaterDate(dateIso);
  }

  // --- Action « Ouvrir un incident » (P-02, RM-08, RM-09) -----------------------------------

  protected readonly libelleOuvrirIncident = $localize`:@@assets.fiche.ouvrirIncident:Ouvrir un incident`;

  /** RM-09 : indisponible sur un actif déjà au rebut — double contrôle, l'API refuse aussi. */
  protected readonly peutOuvrirIncident = computed(() => {
    const actifCourant = this.actif();
    return actifCourant !== undefined && actifCourant.status !== 'Decommissioned';
  });

  protected readonly lienOuvrirIncident = computed(() => ['/tickets/nouveau']);
  protected readonly parametresOuvrirIncident = computed(() => ({ assetId: this.actif()?.id }));

  // --- Action « Mettre au rebut » (P-06, RM-06, RM-07) ------------------------------------------

  protected readonly titreDialogueRebut = $localize`:@@assets.fiche.rebut.titre:Mettre l'actif au rebut`;
  protected readonly messageDialogueRebut = $localize`:@@assets.fiche.rebut.message:Cet équipement sortira du parc actif et son numéro de série restera réservé. Un administrateur pourra le remettre en service.`;
  protected readonly libelleConfirmerRebut = $localize`:@@assets.fiche.rebut.confirmer:Confirmer la mise au rebut`;

  protected readonly dialogueRebutOuvert = signal(false);
  protected readonly enCoursRebut = signal(false);
  protected readonly erreurRebut = signal<string | null>(null);

  /** Visible pour tout utilisateur authentifié, tant que l'actif n'est pas déjà au rebut. */
  protected readonly peutMettreAuRebut = computed(() => {
    const actifCourant = this.actif();
    return actifCourant !== undefined && actifCourant.status !== 'Decommissioned';
  });

  protected ouvrirDialogueRebut(): void {
    this.erreurRebut.set(null);
    this.dialogueRebutOuvert.set(true);
  }

  /**
   * Relais de `(annulation)` : `ConfirmDialog` n'émet déjà plus cet événement pendant `enCours`
   * (voir son garde interne), donc aucune vérification supplémentaire n'est nécessaire ici.
   */
  protected fermerDialogueRebut(): void {
    this.dialogueRebutOuvert.set(false);
    this.erreurRebut.set(null);
  }

  protected confirmerMiseAuRebut(): void {
    const actifCourant = this.actif();
    if (actifCourant === undefined) {
      return;
    }

    this.erreurRebut.set(null);
    this.enCoursRebut.set(true);

    // `takeUntilDestroyed` : sans lui, une navigation hors de cette fiche pendant l'appel
    // laisserait l'abonnement actif, prêt à écrire dans des signaux dont plus personne ne lit
    // la valeur — inoffensif ici (pas de navigation déclenchée par ce callback), mais fermer
    // l'abonnement au même endroit que la remise en service évite toute divergence future.
    this.api
      .decommission(actifCourant.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.enCoursRebut.set(false);
          this.dialogueRebutOuvert.set(false);
          this.recharger();
        },
        error: (erreur: unknown) => {
          this.enCoursRebut.set(false);
          this.erreurRebut.set(this.messageErreurMiseAuRebut(erreur));
        },
      });
  }

  private messageErreurMiseAuRebut(erreur: unknown): string {
    if (!(erreur instanceof ApiError)) {
      return this.messageErreurInattendue;
    }
    if (erreur.kind === 'notFound') {
      return this.messageEquipementIntrouvable;
    }
    if (erreur.kind === 'business') {
      return this.messageIncidentsActifs(erreur.message);
    }
    return erreur.message;
  }

  /**
   * Reconstruit le message imposé par la mission à partir du nombre porté par le message serveur
   * (ex. « Action interdite : l'actif fait l'objet de 2 incident(s) en cours de traitement. »).
   * Une regex simple `/\d+/` suffit : le contrat garantit un entier dans le message. En cas de
   * forme inattendue (contrat modifié sans mise à jour de ce composant), on retombe sur le message
   * brut du serveur — déjà en français — plutôt que sur un texte à trou vide de sens.
   */
  private messageIncidentsActifs(messageServeur: string): string {
    const correspondance = /\d+/.exec(messageServeur);
    if (correspondance === null) {
      return messageServeur;
    }

    const nombre = Number(correspondance[0]);
    return $localize`:@@assets.fiche.rebut.incidentsActifs:Impossible : cet équipement a ${nombre}:nombre: incident(s) en cours. Clôturez-les d'abord.`;
  }

  // --- Action « Remettre en service » (P-06 bis, RM-28, décision 0.4) ---------------------------

  protected readonly titreDialogueRemiseEnService = $localize`:@@assets.fiche.remiseEnService.titre:Remettre l'actif en service`;
  protected readonly messageDialogueRemiseEnService = $localize`:@@assets.fiche.remiseEnService.message:Cet équipement redeviendra utilisable et pourra porter de nouveaux incidents. Indiquez le motif.`;
  protected readonly libelleConfirmerRemiseEnService = $localize`:@@assets.fiche.remiseEnService.confirmer:Confirmer la remise en service`;
  protected readonly libelleChampMotif = $localize`:@@assets.fiche.remiseEnService.motifLabel:Motif de remise en service`;
  protected readonly messageMotifObligatoire = $localize`:@@assets.fiche.remiseEnService.motifObligatoire:Le motif de remise en service est obligatoire.`;
  protected readonly messagesMotif = { motifVide: this.messageMotifObligatoire };

  protected readonly motifRemiseEnService = new FormControl('', {
    nonNullable: true,
    validators: [validerMotifNonVide],
  });

  /** Conteneur du champ projeté, pour y déplacer le focus si le motif est invalide à la soumission. */
  private readonly zoneMotif: Signal<ElementRef<HTMLElement> | undefined> = viewChild('zoneMotif', {
    read: ElementRef,
  });

  protected readonly dialogueRemiseEnServiceOuvert = signal(false);
  protected readonly enCoursRemiseEnService = signal(false);
  protected readonly erreurRemiseEnService = signal<string | null>(null);

  /**
   * `estAdministrateur` est une facilité d'ergonomie, pas une protection (voir le commentaire sur
   * `jwtRoles` ci-dessus) : un rôle falsifié échouerait de toute façon côté API (403), déjà géré
   * par `messageErreurRemiseEnService`.
   */
  protected readonly peutRemettreEnService = computed(() => {
    const actifCourant = this.actif();
    return (
      actifCourant !== undefined &&
      actifCourant.status === 'Decommissioned' &&
      this.jwtRoles.estAdministrateur()
    );
  });

  protected ouvrirDialogueRemiseEnService(): void {
    this.motifRemiseEnService.reset('');
    this.erreurRemiseEnService.set(null);
    this.dialogueRemiseEnServiceOuvert.set(true);
  }

  protected fermerDialogueRemiseEnService(): void {
    this.dialogueRemiseEnServiceOuvert.set(false);
    this.erreurRemiseEnService.set(null);
  }

  /**
   * Validation locale avant tout appel réseau (motif non vide après trim) : la seule protection
   * réelle, faute de validateur serveur dédié pour cette règle (voir `validerMotifNonVide`).
   */
  protected confirmerRemiseEnService(): void {
    if (this.motifRemiseEnService.invalid) {
      this.motifRemiseEnService.markAsTouched();
      // `aria-invalid` n'est écrit dans le DOM qu'au prochain rendu Angular, jamais de façon
      // synchrone avec `markAsTouched()` (zoneless) : `afterNextRender` diffère le déplacement du
      // focus jusqu'à ce que `suivreEtatControle` ait effectivement mis à jour le gabarit.
      afterNextRender(
        () => {
          const conteneur = this.zoneMotif()?.nativeElement;
          if (conteneur !== undefined) {
            focusPremierChampInvalide(conteneur);
          }
        },
        { injector: this.injector },
      );
      return;
    }

    const actifCourant = this.actif();
    if (actifCourant === undefined) {
      return;
    }

    this.erreurRemiseEnService.set(null);
    this.enCoursRemiseEnService.set(true);

    const requete: RestoreAssetToServiceRequest = {
      reason: this.motifRemiseEnService.value.trim(),
    };

    this.api
      .restoreToService(actifCourant.id, requete)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.enCoursRemiseEnService.set(false);
          this.dialogueRemiseEnServiceOuvert.set(false);
          this.recharger();
        },
        error: (erreur: unknown) => {
          this.enCoursRemiseEnService.set(false);
          this.erreurRemiseEnService.set(this.messageErreurRemiseEnService(erreur));
        },
      });
  }

  /**
   * 400 (motif vide résiduel, ou actif qui ne serait plus `Decommissioned`) et 403 (masquage
   * périmé — rappel `angular-security-auth` : l'API tranche indépendamment de l'interface)
   * reçoivent tous deux un message déjà rédigé pour un humain par `errorInterceptor` : rien à
   * reformuler, contrairement au décompte d'incidents de la mise au rebut.
   */
  private messageErreurRemiseEnService(erreur: unknown): string {
    if (!(erreur instanceof ApiError)) {
      return this.messageErreurInattendue;
    }
    if (erreur.kind === 'notFound') {
      return this.messageEquipementIntrouvable;
    }
    return erreur.message;
  }
}
