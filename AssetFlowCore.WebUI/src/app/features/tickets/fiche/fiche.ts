import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  Signal,
  afterNextRender,
  computed,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, ValidationErrors, Validators } from '@angular/forms';
import { TeamsApiService } from '../../../core/api/teams-api.service';
import { TicketsApiService } from '../../../core/api/tickets-api.service';
import { focusPremierChampInvalide } from '../../../shared/forms/focus-invalide';
import { ApiError } from '../../../shared/models/api-error.model';
import { TransferTicketRequest } from '../../../shared/models/ticket.model';
import { Button } from '../../../shared/ui/button/button';
import { Card } from '../../../shared/ui/card/card';
import { ConfirmDialog } from '../../../shared/ui/confirm-dialog/confirm-dialog';
import { ErrorMessage } from '../../../shared/ui/error-message/error-message';
import { EmptyState } from '../../../shared/ui/empty-state/empty-state';
import { OptionSelecteur, SelectField } from '../../../shared/ui/select-field/select-field';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import {
  TicketCriticalityBadge,
  TicketStatusBadge,
} from '../../../shared/ui/status-badges/status-badges';
import { TextareaField } from '../../../shared/ui/textarea-field/textarea-field';

const FORMATEUR_DATE = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'medium', timeZone: 'UTC' });

function formaterDate(dateIso: string): string {
  return FORMATEUR_DATE.format(new Date(dateIso));
}

function validerNonVide(controle: AbstractControl<string>): ValidationErrors | null {
  return controle.value.trim().length === 0 ? { requis: true } : null;
}

/** Vrai pour un 409 : seul cas où le message imposé RM-22 doit remplacer celui du serveur. */
function estConflit(erreur: unknown): boolean {
  return erreur instanceof ApiError && erreur.kind === 'conflict';
}

/**
 * E-05 — Fiche d'un incident.
 *
 * Aucun jeton de concurrence n'est exposé par le contrat (`TicketResponse` n'a pas de
 * `rowVersion`) : un 409 ne peut être anticipé, seulement constaté au retour d'une action. RM-22
 * impose de ne **jamais perdre la saisie en cours** sur ce cas — les boîtes de dialogue de
 * clôture et de transfert restent donc **ouvertes**, avec leur contenu intact, sur un 409 ; seul
 * `recharger()` (qui ne touche qu'à la ressource de l'incident, jamais aux `FormControl`) rafraîchit
 * les données sous-jacentes, sur action explicite de l'utilisateur.
 *
 * `transferHistory` n'est peuplé que par `GET /api/v1/tickets/{id}` (jamais par la liste) : cette
 * fiche est donc la seule source d'affichage de l'historique de routage (RM-21).
 */
@Component({
  selector: 'app-tickets-fiche',
  imports: [
    Button,
    Card,
    ConfirmDialog,
    EmptyState,
    ErrorMessage,
    SelectField,
    Spinner,
    TextareaField,
    TicketCriticalityBadge,
    TicketStatusBadge,
  ],
  templateUrl: './fiche.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Fiche {
  private readonly api = inject(TicketsApiService);
  private readonly teamsApi = inject(TeamsApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly id = input.required<string>();

  private readonly ressource = rxResource({
    params: () => this.id(),
    stream: ({ params }) => this.api.getById(params),
  });

  protected readonly incident = this.ressource.value;
  protected readonly chargement = this.ressource.isLoading;

  protected readonly erreur = computed(() => {
    const erreur = this.ressource.error();
    return erreur instanceof ApiError ? erreur : null;
  });

  /**
   * Message à afficher pour l'échec du chargement initial. Ne relaie **jamais** `erreur.message`
   * tel quel sur un 404 : `errorInterceptor` préfère le `detail` du backend quand il existe
   * (`message: detail || MESSAGES.notFound`), et ce `detail` contient ici littéralement le GUID
   * de l'incident (« Le ticket {id} est introuvable. ») — règle transverse enfreinte sinon
   * (« ne jamais afficher un identifiant technique dans un message destiné à l'utilisateur »).
   * Prend l'erreur déjà réduite au non-nul par le gabarit (`@else if (erreur(); as erreurFiche)`)
   * plutôt qu'un second `computed()` sur `erreur()`, pour ne pas avoir à réaffirmer sa nullité.
   */
  protected messageErreurChargement(erreurCourante: ApiError): string {
    return erreurCourante.kind === 'notFound'
      ? this.messageIncidentIntrouvable
      : erreurCourante.message;
  }

  protected recharger(): void {
    this.ressource.reload();
  }

  protected dateCreation(dateIso: string): string {
    return formaterDate(dateIso);
  }

  protected dateTransfert(dateIso: string): string {
    return formaterDate(dateIso);
  }

  protected readonly libelleChargement = $localize`:@@tickets.fiche.chargement:Chargement de l'incident…`;
  protected readonly titreErreurChargement = $localize`:@@tickets.fiche.erreurChargement.titre:Chargement impossible`;
  protected readonly messageIncidentIntrouvableVide = $localize`:@@tickets.fiche.introuvableVide:Cet incident est introuvable.`;
  protected readonly libelleEquipeNonAssignee = $localize`:@@tickets.fiche.equipeNonAssignee:Non assignée`;
  protected readonly libelleEquipeInconnue = $localize`:@@tickets.fiche.equipeInconnue:Équipe inconnue`;

  // --- Formatage des messages d'erreur communs aux trois actions ----------------------------

  private readonly messageErreurInattendue = $localize`:@@tickets.fiche.erreurInattendue:Une erreur inattendue est survenue.`;
  private readonly messageIncidentIntrouvable = $localize`:@@tickets.fiche.introuvable:Cet incident n'existe plus. Actualisez la liste.`;
  protected readonly messageConflit = $localize`:@@tickets.fiche.conflit:Cet incident a été modifié par quelqu'un d'autre. Rechargez pour voir les dernières données.`;

  private messageErreurAction(erreur: unknown): string {
    if (estConflit(erreur)) {
      return this.messageConflit;
    }
    if (!(erreur instanceof ApiError)) {
      return this.messageErreurInattendue;
    }
    if (erreur.kind === 'notFound') {
      return this.messageIncidentIntrouvable;
    }
    return erreur.message;
  }

  // --- Action « Prendre en charge » (P-03, RM-13, RM-14) -------------------------------------

  protected readonly libellePrendreEnCharge = $localize`:@@tickets.fiche.priseEnCharge.bouton:Prendre en charge`;

  protected readonly enCoursPriseEnCharge = signal(false);
  protected readonly erreurPriseEnCharge = signal<string | null>(null);
  protected readonly conflitPriseEnCharge = signal(false);

  protected readonly peutPrendreEnCharge = computed(() => this.incident()?.status === 'Opened');

  protected prendreEnCharge(): void {
    const incidentCourant = this.incident();
    if (incidentCourant === undefined) {
      return;
    }

    this.erreurPriseEnCharge.set(null);
    this.conflitPriseEnCharge.set(false);
    this.enCoursPriseEnCharge.set(true);

    this.api
      .assign(incidentCourant.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.enCoursPriseEnCharge.set(false);
          this.recharger();
        },
        error: (erreur: unknown) => {
          this.enCoursPriseEnCharge.set(false);
          this.conflitPriseEnCharge.set(estConflit(erreur));
          this.erreurPriseEnCharge.set(this.messageErreurAction(erreur));
        },
      });
  }

  // --- Action « Clôturer » (P-04, RM-15, RM-16, RM-17) ----------------------------------------

  protected readonly titreDialogueCloture = $localize`:@@tickets.fiche.cloture.titre:Clôturer l'incident`;
  protected readonly messageDialogueCloture = $localize`:@@tickets.fiche.cloture.message:Une fois clôturé, cet incident ne pourra plus être rouvert. Si c'est le dernier incident actif de l'équipement, il redeviendra en service.`;
  protected readonly libelleConfirmerCloture = $localize`:@@tickets.fiche.cloture.confirmer:Confirmer la clôture`;
  protected readonly libelleChampCompteRendu = $localize`:@@tickets.fiche.cloture.compteRenduLabel:Compte rendu de résolution`;
  protected readonly messageCompteRenduObligatoire = $localize`:@@tickets.fiche.cloture.compteRenduObligatoire:Le compte rendu de résolution est obligatoire.`;
  protected readonly messagesCompteRendu = { requis: this.messageCompteRenduObligatoire };

  protected readonly compteRendu = new FormControl('', {
    nonNullable: true,
    validators: [validerNonVide],
  });

  /** Conteneur du champ projeté, pour y déplacer le focus si le compte rendu est invalide. */
  private readonly zoneCompteRendu: Signal<ElementRef<HTMLElement> | undefined> = viewChild(
    'zoneCompteRendu',
    { read: ElementRef },
  );

  protected readonly dialogueClotureOuvert = signal(false);
  protected readonly enCoursCloture = signal(false);
  protected readonly erreurCloture = signal<string | null>(null);
  protected readonly conflitCloture = signal(false);

  protected readonly peutCloturer = computed(() => this.incident()?.status === 'InProgress');

  protected ouvrirDialogueCloture(): void {
    this.compteRendu.reset('');
    this.erreurCloture.set(null);
    this.conflitCloture.set(false);
    this.dialogueClotureOuvert.set(true);
  }

  protected fermerDialogueCloture(): void {
    this.dialogueClotureOuvert.set(false);
    this.erreurCloture.set(null);
  }

  protected confirmerCloture(): void {
    if (this.compteRendu.invalid) {
      this.compteRendu.markAsTouched();
      afterNextRender(
        () => {
          const conteneur = this.zoneCompteRendu()?.nativeElement;
          if (conteneur !== undefined) {
            focusPremierChampInvalide(conteneur);
          }
        },
        { injector: this.injector },
      );
      return;
    }

    const incidentCourant = this.incident();
    if (incidentCourant === undefined) {
      return;
    }

    this.erreurCloture.set(null);
    this.conflitCloture.set(false);
    this.enCoursCloture.set(true);

    this.api
      .close(incidentCourant.id, { resolutionComment: this.compteRendu.value.trim() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.enCoursCloture.set(false);
          this.dialogueClotureOuvert.set(false);
          this.recharger();
        },
        error: (erreur: unknown) => {
          this.enCoursCloture.set(false);
          // RM-22 : sur un conflit, la boîte reste ouverte et le compte rendu saisi n'est pas
          // perdu — seul un rechargement explicite (bouton dédié dans le gabarit) rafraîchit
          // l'incident sous-jacent, sans toucher au `FormControl`.
          this.conflitCloture.set(estConflit(erreur));
          this.erreurCloture.set(this.messageErreurAction(erreur));
        },
      });
  }

  // --- Action « Transférer » (P-05, RM-18→RM-20) ----------------------------------------------

  protected readonly titreDialogueTransfert = $localize`:@@tickets.fiche.transfert.titre:Transférer l'incident`;
  protected readonly messageDialogueTransfert = $localize`:@@tickets.fiche.transfert.message:L'incident sera réaffecté à l'équipe choisie ; le motif est conservé dans l'historique de routage.`;
  protected readonly libelleConfirmerTransfert = $localize`:@@tickets.fiche.transfert.confirmer:Confirmer le transfert`;
  protected readonly libelleChampEquipeCible = $localize`:@@tickets.fiche.transfert.equipeCibleLabel:Équipe cible`;
  protected readonly libelleChampMotifTransfert = $localize`:@@tickets.fiche.transfert.motifLabel:Motif du transfert`;
  protected readonly messageMotifTransfertObligatoire = $localize`:@@tickets.fiche.transfert.motifObligatoire:Le motif du transfert est obligatoire.`;
  protected readonly messagesMotifTransfert = { requis: this.messageMotifTransfertObligatoire };
  protected readonly messageAucuneAutreEquipe = $localize`:@@tickets.fiche.transfert.aucuneAutreEquipe:Aucune autre équipe active n'est disponible pour un transfert.`;

  protected readonly equipeCible = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required],
  });
  protected readonly motifTransfert = new FormControl('', {
    nonNullable: true,
    validators: [validerNonVide],
  });

  private readonly ressourceEquipes = rxResource<readonly { readonly name: string }[], void>({
    stream: () => this.teamsApi.getAll(true),
    defaultValue: [],
  });

  /** RM-19 : l'équipe déjà assignée est exclue du sélecteur — un transfert vers elle est refusé. */
  protected readonly optionsEquipesCibles = computed<readonly OptionSelecteur<string>[]>(() => {
    const equipeActuelle = this.incident()?.assignedTeamName;
    return this.ressourceEquipes
      .value()
      .filter((equipe) => equipe.name !== equipeActuelle)
      .map((equipe) => ({ valeur: equipe.name, libelle: equipe.name }));
  });

  /** Conteneur des champs projetés, pour y déplacer le focus si le formulaire est invalide. */
  private readonly zoneTransfert: Signal<ElementRef<HTMLElement> | undefined> = viewChild(
    'zoneTransfert',
    { read: ElementRef },
  );

  protected readonly dialogueTransfertOuvert = signal(false);
  protected readonly enCoursTransfert = signal(false);
  protected readonly erreurTransfert = signal<string | null>(null);
  protected readonly conflitTransfert = signal(false);

  protected readonly peutTransferer = computed(
    () => this.incident() !== undefined && this.incident()?.status !== 'Closed',
  );

  protected ouvrirDialogueTransfert(): void {
    this.equipeCible.reset('');
    this.motifTransfert.reset('');
    this.erreurTransfert.set(null);
    this.conflitTransfert.set(false);
    this.dialogueTransfertOuvert.set(true);
  }

  protected fermerDialogueTransfert(): void {
    this.dialogueTransfertOuvert.set(false);
    this.erreurTransfert.set(null);
  }

  protected confirmerTransfert(): void {
    if (this.optionsEquipesCibles().length === 0) {
      return;
    }

    this.equipeCible.markAsTouched();
    this.motifTransfert.markAsTouched();
    if (this.equipeCible.invalid || this.motifTransfert.invalid) {
      afterNextRender(
        () => {
          const conteneur = this.zoneTransfert()?.nativeElement;
          if (conteneur !== undefined) {
            focusPremierChampInvalide(conteneur);
          }
        },
        { injector: this.injector },
      );
      return;
    }

    const incidentCourant = this.incident();
    if (incidentCourant === undefined) {
      return;
    }

    this.erreurTransfert.set(null);
    this.conflitTransfert.set(false);
    this.enCoursTransfert.set(true);

    const requete: TransferTicketRequest = {
      targetTeam: this.equipeCible.value,
      reason: this.motifTransfert.value.trim(),
    };

    this.api
      .transfer(incidentCourant.id, requete)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.enCoursTransfert.set(false);
          this.dialogueTransfertOuvert.set(false);
          this.recharger();
        },
        error: (erreur: unknown) => {
          this.enCoursTransfert.set(false);
          this.conflitTransfert.set(estConflit(erreur));
          this.erreurTransfert.set(this.messageErreurAction(erreur));
        },
      });
  }
}
