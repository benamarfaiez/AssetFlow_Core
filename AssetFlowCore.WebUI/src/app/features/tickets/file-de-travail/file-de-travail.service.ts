import { Injectable, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TicketsApiService } from '../../../core/api/tickets-api.service';
import { ApiError } from '../../../shared/models/api-error.model';
import { PagedResult } from '../../../shared/models/paged-result.model';
import {
  TicketCriticality,
  TicketResponse,
  TicketSearchParams,
  TicketStatus,
} from '../../../shared/models/ticket.model';

const TAILLE_DE_PAGE = 20;

const RESULTAT_VIDE: PagedResult<TicketResponse> = {
  items: [],
  page: 1,
  pageSize: TAILLE_DE_PAGE,
  totalCount: 0,
  totalPages: 0,
};

/**
 * État de la file de travail (E-06). Propre à cet écran — fourni au niveau du composant
 * `FileDeTravail`, sans besoin d'être partagé (à la différence d'`InventaireService` côté
 * `assets` : `GET /api/v1/tickets` est paginé/trié/filtré **côté serveur**, donc un incident
 * nouvellement créé n'a pas de position évidente dans une page déjà chargée — `Formulaire`
 * navigue vers la fiche du ticket créé plutôt que de tenter de mettre à jour cette liste).
 *
 * `params` rend `rxResource` réactif aux changements de filtre/page — cf. `ResourceLoaderParams`.
 * Changer un filtre revient à la page 1 : rester sur une page devenue hors bornes afficherait
 * l'état « vide » sans que l'utilisateur comprenne pourquoi (voir `FileDeTravail`).
 */
@Injectable()
export class FileDeTravailService {
  private readonly api = inject(TicketsApiService);

  private readonly _statut = signal<TicketStatus | ''>('');
  private readonly _criticite = signal<TicketCriticality | ''>('');
  private readonly _page = signal(1);

  readonly statut = this._statut.asReadonly();
  readonly criticite = this._criticite.asReadonly();
  readonly page = this._page.asReadonly();
  readonly tailleDePage = TAILLE_DE_PAGE;

  private readonly ressource = rxResource<PagedResult<TicketResponse>, TicketSearchParams>({
    // `statut`/`criticite` capturés en variables locales : TypeScript ne peut pas affiner le
    // type de retour d'un appel de signal répété dans un même objet littéral (chaque appel est
    // vu comme potentiellement distinct), d'où l'étroitesse sinon vers `TicketStatus | undefined`
    // attendue par `TicketSearchParams` alors que le type porté est `TicketStatus | '' `.
    params: () => {
      const statut = this._statut();
      const criticite = this._criticite();
      return {
        status: statut === '' ? undefined : statut,
        criticality: criticite === '' ? undefined : criticite,
        page: this._page(),
        pageSize: TAILLE_DE_PAGE,
      };
    },
    stream: ({ params }) => this.api.search(params),
    defaultValue: RESULTAT_VIDE,
  });

  readonly resultat = this.ressource.value;
  readonly chargement = this.ressource.isLoading;

  readonly erreur = computed(() => {
    const erreur = this.ressource.error();
    return erreur instanceof ApiError ? erreur : null;
  });

  recharger(): void {
    this.ressource.reload();
  }

  /** Remet la pagination à 1 : un filtre changé sur une page avancée afficherait sinon « vide ». */
  definirStatut(valeur: TicketStatus | ''): void {
    this._statut.set(valeur);
    this._page.set(1);
  }

  definirCriticite(valeur: TicketCriticality | ''): void {
    this._criticite.set(valeur);
    this._page.set(1);
  }

  allerALaPage(page: number): void {
    this._page.set(page);
  }
}
