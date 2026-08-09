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
  signal,
  viewChild,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, Validators } from '@angular/forms';
import { JwtRolesService } from '../../core/auth/jwt-roles.service';
import { TeamsApiService } from '../../core/api/teams-api.service';
import { focusPremierChampInvalide } from '../../shared/forms/focus-invalide';
import { optionsDepuisLibelles } from '../../shared/forms/options-depuis-libelles';
import {
  LIBELLES_ASSET_TYPE,
  LIBELLES_TICKET_CRITICALITY,
  traduire,
} from '../../shared/i18n/libelles';
import { ASSET_TYPES, AssetType } from '../../shared/models/asset.model';
import { ApiError } from '../../shared/models/api-error.model';
import { TICKET_CRITICALITIES, TicketCriticality } from '../../shared/models/ticket.model';
import { TeamResponse } from '../../shared/models/team.model';
import { AssetTypeLabelPipe, TicketCriticalityLabelPipe } from '../../shared/pipes/libelles.pipe';
import { Badge } from '../../shared/ui/badge/badge';
import { Button } from '../../shared/ui/button/button';
import { Card } from '../../shared/ui/card/card';
import { ConfirmDialog } from '../../shared/ui/confirm-dialog/confirm-dialog';
import { ColonneTable, DataTable } from '../../shared/ui/data-table/data-table';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { ErrorMessage } from '../../shared/ui/error-message/error-message';
import { Modal } from '../../shared/ui/modal/modal';
import { OptionSelecteur, SelectField } from '../../shared/ui/select-field/select-field';
import { Spinner } from '../../shared/ui/spinner/spinner';
import { TextField } from '../../shared/ui/text-field/text-field';
import { TextareaField } from '../../shared/ui/textarea-field/textarea-field';

/** Les 9 combinaisons (type d'actif × criticité) que le référentiel doit couvrir (RM-12, §4.4). */
const COMBINAISONS: readonly {
  readonly assetType: AssetType;
  readonly ticketCriticality: TicketCriticality;
}[] = ASSET_TYPES.flatMap((assetType) =>
  TICKET_CRITICALITIES.map((ticketCriticality) => ({ assetType, ticketCriticality })),
);

interface CombinaisonCouverture {
  readonly assetType: AssetType;
  readonly ticketCriticality: TicketCriticality;
  readonly couverte: boolean;
}

/** Reconnaît le refus d'unicité du nom (RM-23) au contenu du message, faute de dictionnaire
 * `errors` sur cette route : `CreateTeamCommandHandler`/`UpdateTeamCommandHandler` lèvent tous
 * deux une `DomainException` (« Une équipe nommée 'X' existe déjà. »), jamais une erreur de champ. */
const MOTIF_NOM_DEJA_PRIS = /existe déjà/i;

/**
 * E-07 — Administration des équipes (P-08).
 *
 * Écran unique (à la différence d'`assets`/`tickets`) : liste, création, modification partielle,
 * suppression et bascule d'activation y cohabitent. Création et édition partagent une seule
 * fenêtre modale et un seul formulaire réactif (`equipeEnEdition() === null` distingue les deux
 * modes) plutôt qu'une route dédiée — l'écran ne navigue jamais.
 *
 * `GET /api/v1/teams` (`onlyActive=false`, pour voir aussi les équipes désactivées) n'a aucune
 * restriction de rôle : la liste et la **couverture des 9 combinaisons** sont donc visibles de
 * tout utilisateur authentifié. Seules les actions de mutation (créer, modifier, supprimer,
 * activer, désactiver) sont réservées au rôle `Administrateur` côté API
 * (`[Authorize(Roles = Roles.Administrateur)]` sur `TeamsController`) : `estAdministrateur()`
 * masque ces actions par ergonomie, à l'identique de `assets/fiche.ts` — jamais la seule
 * protection réelle.
 *
 * L'activation est une action directe (non destructive, comme « Prendre en charge » côté
 * tickets) ; la désactivation passe par `app-confirm-dialog` et avertit en plus, dans le
 * contenu projeté, quand elle retirerait la **dernière** équipe active d'une combinaison
 * (RM-31) — ce contrôle n'est fait que côté client, l'API ne le signale pas.
 *
 * Chaque mutation (créer/modifier/supprimer/activer/désactiver) recharge la liste via
 * `recharger()` plutôt que de fusionner la réponse en mémoire, à la différence délibérée
 * d'`assets/formulaire.ts` : `CachedTeamRepository` **invalide bien les deux listes en cache**
 * sur chacun de ces cinq chemins d'écriture (`AddAsync`/`UpdateAsync`/`RemoveAsync`, tous appelés
 * explicitement par leurs handlers respectifs — voir `ActivateTeamCommandHandler` notamment),
 * contrairement au cache d'actifs qu'aucune écriture n'invalide. Un rechargement est donc ici
 * fiable, jamais périmé pendant 5 minutes.
 */
@Component({
  selector: 'app-teams',
  imports: [
    AssetTypeLabelPipe,
    Badge,
    Button,
    Card,
    ConfirmDialog,
    DataTable,
    EmptyState,
    ErrorMessage,
    Modal,
    SelectField,
    Spinner,
    TextField,
    TextareaField,
    TicketCriticalityLabelPipe,
  ],
  templateUrl: './teams.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Teams {
  private readonly api = inject(TeamsApiService);
  private readonly jwtRoles = inject(JwtRolesService);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  protected readonly estAdministrateur = this.jwtRoles.estAdministrateur;

  private readonly ressourceEquipes = rxResource<readonly TeamResponse[], void>({
    stream: () => this.api.getAll(false),
    defaultValue: [],
  });

  protected readonly equipes = this.ressourceEquipes.value;
  protected readonly chargement = this.ressourceEquipes.isLoading;

  protected readonly erreur = computed(() => {
    const erreur = this.ressourceEquipes.error();
    return erreur instanceof ApiError ? erreur : null;
  });

  protected recharger(): void {
    this.ressourceEquipes.reload();
  }

  private messageErreur(erreur: unknown): string {
    return erreur instanceof ApiError ? erreur.message : this.messageErreurInattendue;
  }

  // --- Couverture des 9 combinaisons (5.C.3, RM-12) -------------------------------------------

  /** Une équipe désactivée ne compte jamais comme couvrante (décision 0.6). */
  protected readonly couverture = computed<readonly CombinaisonCouverture[]>(() => {
    const equipesActives = this.equipes().filter((equipe) => equipe.isActive);
    return COMBINAISONS.map((combinaison) => ({
      ...combinaison,
      couverte: equipesActives.some(
        (equipe) =>
          equipe.assetType === combinaison.assetType &&
          equipe.ticketCriticality === combinaison.ticketCriticality,
      ),
    }));
  });

  protected readonly nombreCombinaisonsNonCouvertes = computed(
    () => this.couverture().filter((combinaison) => !combinaison.couverte).length,
  );

  protected readonly cleCombinaison = (combinaison: CombinaisonCouverture): string =>
    `${combinaison.assetType}-${combinaison.ticketCriticality}`;

  // --- Colonnes du tableau -------------------------------------------------------------------

  private readonly gabaritEtat =
    viewChild.required<TemplateRef<{ $implicit: TeamResponse }>>('gabaritEtat');
  private readonly gabaritActions =
    viewChild.required<TemplateRef<{ $implicit: TeamResponse }>>('gabaritActions');

  protected readonly colonnes = computed<readonly ColonneTable<TeamResponse>[]>(() => [
    { cle: 'nom', entete: this.libelleColonneNom, valeur: (equipe) => equipe.name },
    {
      cle: 'type',
      entete: this.libelleColonneType,
      valeur: (equipe) => traduire(LIBELLES_ASSET_TYPE, equipe.assetType),
    },
    {
      cle: 'criticite',
      entete: this.libelleColonneCriticite,
      valeur: (equipe) => traduire(LIBELLES_TICKET_CRITICALITY, equipe.ticketCriticality),
    },
    {
      cle: 'etat',
      entete: this.libelleColonneEtat,
      valeur: (equipe) => (equipe.isActive ? this.libelleActive : this.libelleInactive),
      gabarit: this.gabaritEtat(),
    },
    {
      cle: 'actions',
      entete: this.libelleColonneActions,
      valeur: () => '',
      gabarit: this.gabaritActions(),
    },
  ]);

  protected readonly cleEquipe = (equipe: TeamResponse): string => equipe.id;

  // --- Activation directe (non destructive) -------------------------------------------------

  protected readonly idEnCoursActivation = signal<string | null>(null);
  protected readonly erreurActivation = signal<string | null>(null);

  protected activerEquipe(equipe: TeamResponse): void {
    this.erreurActivation.set(null);
    this.idEnCoursActivation.set(equipe.id);

    this.api
      .activate(equipe.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.idEnCoursActivation.set(null);
          this.recharger();
        },
        error: (erreur: unknown) => {
          this.idEnCoursActivation.set(null);
          this.erreurActivation.set(this.messageErreur(erreur));
        },
      });
  }

  // --- Désactivation (confirmation + avertissement RM-31) -----------------------------------

  protected readonly equipeADesactiver = signal<TeamResponse | null>(null);
  protected readonly enCoursDesactivation = signal(false);
  protected readonly erreurDesactivation = signal<string | null>(null);

  /**
   * RM-31 : vrai si l'équipe visée par `equipeADesactiver()` est la seule équipe encore active
   * de sa combinaison. `computed()` plutôt qu'une méthode prenant `equipe` en paramètre — la
   * seule équipe jamais interrogée ici est celle du dialogue ouvert, déjà portée par un signal.
   */
  protected readonly avertirDerniereEquipeActiveDuCouple = computed(() => {
    const equipe = this.equipeADesactiver();
    if (equipe === null || !equipe.isActive) {
      return false;
    }
    return (
      this.equipes().filter(
        (autre) =>
          autre.isActive &&
          autre.assetType === equipe.assetType &&
          autre.ticketCriticality === equipe.ticketCriticality,
      ).length === 1
    );
  });

  protected ouvrirDesactivation(equipe: TeamResponse): void {
    this.erreurDesactivation.set(null);
    this.equipeADesactiver.set(equipe);
  }

  protected fermerDesactivation(): void {
    this.equipeADesactiver.set(null);
    this.erreurDesactivation.set(null);
  }

  protected confirmerDesactivation(): void {
    const equipe = this.equipeADesactiver();
    if (equipe === null) {
      return;
    }

    this.erreurDesactivation.set(null);
    this.enCoursDesactivation.set(true);

    this.api
      .deactivate(equipe.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.enCoursDesactivation.set(false);
          this.equipeADesactiver.set(null);
          this.recharger();
        },
        error: (erreur: unknown) => {
          this.enCoursDesactivation.set(false);
          this.erreurDesactivation.set(this.messageErreur(erreur));
        },
      });
  }

  // --- Suppression (EF-26, RM-25) ------------------------------------------------------------

  protected readonly equipeASupprimer = signal<TeamResponse | null>(null);
  protected readonly enCoursSuppression = signal(false);
  protected readonly erreurSuppression = signal<string | null>(null);

  protected ouvrirSuppression(equipe: TeamResponse): void {
    this.erreurSuppression.set(null);
    this.equipeASupprimer.set(equipe);
  }

  protected fermerSuppression(): void {
    this.equipeASupprimer.set(null);
    this.erreurSuppression.set(null);
  }

  protected confirmerSuppression(): void {
    const equipe = this.equipeASupprimer();
    if (equipe === null) {
      return;
    }

    this.erreurSuppression.set(null);
    this.enCoursSuppression.set(true);

    this.api
      .delete(equipe.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.enCoursSuppression.set(false);
          this.equipeASupprimer.set(null);
          this.recharger();
        },
        error: (erreur: unknown) => {
          this.enCoursSuppression.set(false);
          this.erreurSuppression.set(this.messageErreur(erreur));
        },
      });
  }

  // --- Création / édition (EF-23, EF-25, RM-27) ----------------------------------------------

  protected readonly optionsType = optionsDepuisLibelles(ASSET_TYPES, LIBELLES_ASSET_TYPE);
  protected readonly optionsCriticite: readonly OptionSelecteur<TicketCriticality>[] =
    optionsDepuisLibelles(TICKET_CRITICALITIES, LIBELLES_TICKET_CRITICALITY);

  protected readonly formulaire = this.fb.group({
    name: this.fb.control('', [Validators.required, Validators.maxLength(100)]),
    assetType: this.fb.control<AssetType | ''>('', [Validators.required]),
    ticketCriticality: this.fb.control<TicketCriticality | ''>('', [Validators.required]),
    description: this.fb.control('', [Validators.maxLength(500)]),
  });

  /**
   * Non requis, à la différence d'`assets/formulaire.ts`/`tickets/formulaire.ts` : ce `<form>`
   * est projeté dans `app-modal`, qui ne rend son contenu que si `ouverte()` est vrai (comme
   * `zoneCompteRendu`/`zoneTransfert` côté `tickets/fiche.ts`) — il n'existe donc pas tant que la
   * fenêtre n'a jamais été ouverte.
   */
  private readonly elementFormulaire: Signal<ElementRef<HTMLFormElement> | undefined> = viewChild(
    'elementFormulaire',
    { read: ElementRef },
  );

  /** `null` : création. Sinon, équipe en cours de modification partielle (RM-27). */
  protected readonly equipeEnEdition = signal<TeamResponse | null>(null);

  protected readonly dialogueFormulaireOuvert = signal(false);
  protected readonly enCoursFormulaire = signal(false);
  protected readonly erreurFormulaire = signal<string | null>(null);

  protected readonly titreDialogueFormulaire = computed(() =>
    this.equipeEnEdition() === null ? this.titreCreation : this.titreEdition,
  );

  protected ouvrirCreation(): void {
    this.equipeEnEdition.set(null);
    this.formulaire.reset({ name: '', assetType: '', ticketCriticality: '', description: '' });
    this.erreurFormulaire.set(null);
    this.dialogueFormulaireOuvert.set(true);
  }

  protected ouvrirEdition(equipe: TeamResponse): void {
    this.equipeEnEdition.set(equipe);
    this.formulaire.setValue({
      name: equipe.name,
      assetType: equipe.assetType,
      ticketCriticality: equipe.ticketCriticality,
      description: equipe.description ?? '',
    });
    this.erreurFormulaire.set(null);
    this.dialogueFormulaireOuvert.set(true);
  }

  protected fermerDialogueFormulaire(): void {
    this.dialogueFormulaireOuvert.set(false);
    this.erreurFormulaire.set(null);
  }

  private deplacerFocusVersChampInvalide(): void {
    afterNextRender(
      () => {
        const conteneur = this.elementFormulaire()?.nativeElement;
        if (conteneur !== undefined) {
          focusPremierChampInvalide(conteneur);
        }
      },
      { injector: this.injector },
    );
  }

  protected soumettreFormulaire(evenement: SubmitEvent): void {
    evenement.preventDefault();

    if (this.formulaire.invalid) {
      this.formulaire.markAllAsTouched();
      this.deplacerFocusVersChampInvalide();
      return;
    }

    const valeur = this.formulaire.getRawValue();
    if (valeur.assetType === '' || valeur.ticketCriticality === '') {
      return;
    }

    this.erreurFormulaire.set(null);
    this.enCoursFormulaire.set(true);

    const equipeEnEdition = this.equipeEnEdition();
    const requete = {
      name: valeur.name.trim(),
      assetType: valeur.assetType,
      ticketCriticality: valeur.ticketCriticality,
      description: valeur.description.trim() === '' ? null : valeur.description.trim(),
    };

    const appel =
      equipeEnEdition === null
        ? this.api.create(requete)
        : this.api.update(equipeEnEdition.id, requete);

    appel.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.enCoursFormulaire.set(false);
        this.dialogueFormulaireOuvert.set(false);
        this.recharger();
      },
      error: (erreur: unknown) => {
        this.enCoursFormulaire.set(false);
        this.gererErreurFormulaire(erreur);
      },
    });
  }

  private gererErreurFormulaire(erreur: unknown): void {
    if (!(erreur instanceof ApiError)) {
      this.erreurFormulaire.set(this.messageErreurInattendue);
      return;
    }

    if (erreur.kind === 'business' && MOTIF_NOM_DEJA_PRIS.test(erreur.message)) {
      this.formulaire.controls.name.setErrors({ serveur: erreur.message });
      this.deplacerFocusVersChampInvalide();
      return;
    }

    this.erreurFormulaire.set(erreur.message);
  }

  // --- Textes localisés ------------------------------------------------------------------------

  protected readonly libelleChargement = $localize`:@@teams.chargement:Chargement des équipes…`;
  protected readonly titreErreurChargement = $localize`:@@teams.erreurChargement.titre:Chargement impossible`;
  protected readonly titreVide = $localize`:@@teams.vide.titre:Aucune équipe enregistrée.`;
  protected readonly descriptionVide = $localize`:@@teams.vide.description:Créez la première équipe pour permettre l'ouverture d'incidents.`;
  protected readonly legendeTable = $localize`:@@teams.table.legende:Équipes`;
  protected readonly libelleCreer = $localize`:@@teams.creer:Créer une équipe`;

  protected readonly libelleTitre = $localize`:@@teams.titre:Équipes`;
  protected readonly libelleTitreCouverture = $localize`:@@teams.couverture.titre:Couverture des combinaisons type × criticité`;
  protected readonly libelleCouverte = $localize`:@@teams.couverture.couverte:Couverte`;
  protected readonly libelleNonCouverte = $localize`:@@teams.couverture.nonCouverte:Non couverte`;

  private readonly libelleColonneNom = $localize`:@@teams.colonne.nom:Nom`;
  private readonly libelleColonneType = $localize`:@@teams.colonne.type:Type d'actif`;
  private readonly libelleColonneCriticite = $localize`:@@teams.colonne.criticite:Criticité`;
  private readonly libelleColonneEtat = $localize`:@@teams.colonne.etat:État`;
  private readonly libelleColonneActions = $localize`:@@teams.colonne.actions:Actions`;
  protected readonly libelleActive = $localize`:@@teams.etat.active:Active`;
  protected readonly libelleInactive = $localize`:@@teams.etat.inactive:Désactivée`;

  protected readonly libelleModifier = $localize`:@@teams.actions.modifier:Modifier`;
  protected readonly libelleActiver = $localize`:@@teams.actions.activer:Activer`;
  protected readonly libelleDesactiver = $localize`:@@teams.actions.desactiver:Désactiver`;
  protected readonly libelleSupprimer = $localize`:@@teams.actions.supprimer:Supprimer`;

  protected readonly titreDialogueSuppression = $localize`:@@teams.suppression.titre:Supprimer l'équipe`;
  protected readonly messageDialogueSuppression = $localize`:@@teams.suppression.message:Cette équipe sera définitivement supprimée. L'opération est refusée si un incident, même clôturé, la référence encore.`;
  protected readonly libelleConfirmerSuppression = $localize`:@@teams.suppression.confirmer:Confirmer la suppression`;

  protected readonly titreDialogueDesactivation = $localize`:@@teams.desactivation.titre:Désactiver l'équipe`;
  protected readonly messageDialogueDesactivation = $localize`:@@teams.desactivation.message:Cette équipe cessera de recevoir de nouveaux incidents et disparaîtra des sélecteurs. Son historique est conservé et l'opération reste réversible.`;
  protected readonly libelleConfirmerDesactivation = $localize`:@@teams.desactivation.confirmer:Confirmer la désactivation`;
  protected readonly messageDerniereEquipeCouple = $localize`:@@teams.desactivation.derniereEquipe:C'est la dernière équipe active de cette combinaison : l'ouverture d'un incident de ce type et de cette criticité deviendra impossible tant qu'aucune équipe active ne la couvre à nouveau.`;

  protected readonly titreCreation = $localize`:@@teams.formulaire.titreCreation:Créer une équipe`;
  protected readonly titreEdition = $localize`:@@teams.formulaire.titreEdition:Modifier l'équipe`;
  protected readonly libelleChampNom = $localize`:@@teams.formulaire.champNom:Nom`;
  protected readonly libelleChampType = $localize`:@@teams.formulaire.champType:Type d'actif`;
  protected readonly libelleChampCriticite = $localize`:@@teams.formulaire.champCriticite:Criticité`;
  protected readonly libelleChampDescription = $localize`:@@teams.formulaire.champDescription:Description`;
  protected readonly libelleAnnuler = $localize`:@@teams.formulaire.annuler:Annuler`;
  protected readonly libelleEnregistrer = $localize`:@@teams.formulaire.enregistrer:Enregistrer`;

  private readonly messageErreurInattendue = $localize`:@@teams.erreurInattendue:Une erreur inattendue est survenue.`;

  protected messageCombinaisonsNonCouvertes(nombre: number): string {
    return nombre === 1
      ? $localize`:@@teams.couverture.alerte.singulier:1 combinaison type × criticité n'est couverte par aucune équipe active : l'ouverture d'incident correspondante échouera.`
      : $localize`:@@teams.couverture.alerte.pluriel:${nombre}:nombre: combinaisons type × criticité ne sont couvertes par aucune équipe active : l'ouverture d'incident correspondante échouera.`;
  }
}
