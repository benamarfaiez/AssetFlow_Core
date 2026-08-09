import {
  ChangeDetectionStrategy,
  Component,
  TemplateRef,
  computed,
  effect,
  inject,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { optionsDepuisLibelles } from '../../../shared/forms/options-depuis-libelles';
import {
  LIBELLES_TICKET_CRITICALITY,
  LIBELLES_TICKET_STATUS,
  traduire,
} from '../../../shared/i18n/libelles';
import {
  TICKET_CRITICALITIES,
  TICKET_STATUSES,
  TicketCriticality,
  TicketResponse,
  TicketStatus,
} from '../../../shared/models/ticket.model';
import { ColonneTable, DataTable } from '../../../shared/ui/data-table/data-table';
import { EmptyState } from '../../../shared/ui/empty-state/empty-state';
import { ErrorMessage } from '../../../shared/ui/error-message/error-message';
import { OptionSelecteur, SelectField } from '../../../shared/ui/select-field/select-field';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import {
  TicketCriticalityBadge,
  TicketStatusBadge,
} from '../../../shared/ui/status-badges/status-badges';
import { FileDeTravailService } from './file-de-travail.service';

const FORMATEUR_DATE = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'medium', timeZone: 'UTC' });

/**
 * E-06 — File de travail des incidents.
 *
 * Filtres et pagination sont **entièrement délégués au serveur** (`GET /api/v1/tickets`),
 * contrairement à l'inventaire (`E-01`) qui filtre côté client faute de support serveur — voir
 * `FileDeTravailService`. Une page au-delà de la dernière, ou un filtre sans correspondance,
 * renvoie une liste **vide sans erreur** (`items: []`, `totalCount` inchangé) : traité comme
 * l'état « vide » du Lot 5, jamais comme une erreur.
 */
@Component({
  selector: 'app-tickets-file-de-travail',
  imports: [
    DataTable,
    EmptyState,
    ErrorMessage,
    RouterLink,
    SelectField,
    Spinner,
    TicketCriticalityBadge,
    TicketStatusBadge,
  ],
  providers: [FileDeTravailService],
  templateUrl: './file-de-travail.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FileDeTravail {
  private readonly etat = inject(FileDeTravailService);

  protected readonly resultat = this.etat.resultat;
  protected readonly chargement = this.etat.chargement;
  protected readonly erreur = this.etat.erreur;
  protected readonly tailleDePage = this.etat.tailleDePage;

  protected recharger(): void {
    this.etat.recharger();
  }

  // --- Filtres (délégués au serveur) --------------------------------------------------------

  protected readonly filtreStatut = new FormControl<TicketStatus | ''>('', { nonNullable: true });
  protected readonly filtreCriticite = new FormControl<TicketCriticality | ''>('', {
    nonNullable: true,
  });

  private readonly valeurFiltreStatut = toSignal(this.filtreStatut.valueChanges, {
    initialValue: this.filtreStatut.value,
  });
  private readonly valeurFiltreCriticite = toSignal(this.filtreCriticite.valueChanges, {
    initialValue: this.filtreCriticite.value,
  });

  protected readonly optionsFiltreStatut: readonly OptionSelecteur<TicketStatus | ''>[] = [
    { valeur: '', libelle: $localize`:@@tickets.file.filtreStatut.tous:Tous les états` },
    ...optionsDepuisLibelles(TICKET_STATUSES, LIBELLES_TICKET_STATUS),
  ];

  protected readonly optionsFiltreCriticite: readonly OptionSelecteur<TicketCriticality | ''>[] = [
    { valeur: '', libelle: $localize`:@@tickets.file.filtreCriticite.tous:Toutes les criticités` },
    ...optionsDepuisLibelles(TICKET_CRITICALITIES, LIBELLES_TICKET_CRITICALITY),
  ];

  constructor() {
    // Effets de bord réels (répercuter un filtre sur le service porteur de la ressource), pas une
    // dérivation d'état : `computed()` serait ici un contresens, il ne peut pas appeler de méthode.
    effect(() => this.etat.definirStatut(this.valeurFiltreStatut()));
    effect(() => this.etat.definirCriticite(this.valeurFiltreCriticite()));
  }

  // --- Pagination --------------------------------------------------------------------------

  protected readonly pageCourante = computed(() => this.resultat().page);
  protected readonly totalPages = computed(() => this.resultat().totalPages);
  protected readonly totalElements = computed(() => this.resultat().totalCount);

  protected readonly peutReculer = computed(() => this.pageCourante() > 1);
  protected readonly peutAvancer = computed(() => this.pageCourante() < this.totalPages());

  protected pageSuivante(): void {
    this.etat.allerALaPage(this.pageCourante() + 1);
  }

  protected pagePrecedente(): void {
    this.etat.allerALaPage(this.pageCourante() - 1);
  }

  // --- Colonnes du tableau -----------------------------------------------------------------

  private readonly gabaritCriticite =
    viewChild.required<TemplateRef<{ $implicit: TicketResponse }>>('gabaritCriticite');
  private readonly gabaritStatut =
    viewChild.required<TemplateRef<{ $implicit: TicketResponse }>>('gabaritStatut');
  private readonly gabaritTitre =
    viewChild.required<TemplateRef<{ $implicit: TicketResponse }>>('gabaritTitre');

  protected readonly colonnes = computed<readonly ColonneTable<TicketResponse>[]>(() => [
    {
      cle: 'titre',
      entete: this.libelleColonneTitre,
      valeur: (ticket) => ticket.title,
      gabarit: this.gabaritTitre(),
    },
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
      gabarit: this.gabaritStatut(),
    },
    {
      cle: 'equipe',
      entete: this.libelleColonneEquipe,
      valeur: (ticket) => ticket.assignedTeamName || this.libelleEquipeNonAssignee,
    },
    {
      cle: 'date',
      entete: this.libelleColonneDate,
      valeur: (ticket) => FORMATEUR_DATE.format(new Date(ticket.createdAt)),
    },
  ]);

  protected readonly cleTicket = (ticket: TicketResponse): string => ticket.id;

  // --- Textes localisés --------------------------------------------------------------------

  protected readonly libelleChargement = $localize`:@@tickets.file.chargement:Chargement de la file de travail…`;
  protected readonly titreErreur = $localize`:@@tickets.file.erreur.titre:Chargement impossible`;
  protected readonly titreVide = $localize`:@@tickets.file.vide.titre:Aucun incident ne correspond à ces filtres.`;
  protected readonly legendeTable = $localize`:@@tickets.file.table.legende:File de travail des incidents`;
  protected readonly libelleOuvrirIncident = $localize`:@@tickets.file.ouvrirIncident:Ouvrir un incident`;
  protected readonly libelleFiltreStatut = $localize`:@@tickets.file.filtreStatut.label:État`;
  protected readonly libelleFiltreCriticite = $localize`:@@tickets.file.filtreCriticite.label:Criticité`;
  protected readonly libellePagePrecedente = $localize`:@@tickets.file.pagination.precedente:Page précédente`;
  protected readonly libellePageSuivante = $localize`:@@tickets.file.pagination.suivante:Page suivante`;

  private readonly libelleEquipeNonAssignee = $localize`:@@tickets.file.equipeNonAssignee:Non assignée`;
  private readonly libelleColonneTitre = $localize`:@@tickets.file.colonne.titre:Titre`;
  private readonly libelleColonneCriticite = $localize`:@@tickets.file.colonne.criticite:Criticité`;
  private readonly libelleColonneStatut = $localize`:@@tickets.file.colonne.statut:État`;
  private readonly libelleColonneEquipe = $localize`:@@tickets.file.colonne.equipe:Équipe`;
  private readonly libelleColonneDate = $localize`:@@tickets.file.colonne.date:Ouvert le`;

  /** Texte « Page X sur Y (N incidents) », construit en une seule fois pour le gabarit. */
  protected readonly libellePagination = computed(
    () =>
      $localize`:@@tickets.file.pagination.resume:Page ${this.pageCourante()}:page: sur ${this.totalPages()}:totalPages: (${this.totalElements()}:total: incident(s))`,
  );
}
