import {
  ChangeDetectionStrategy,
  Component,
  TemplateRef,
  computed,
  inject,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { optionsDepuisLibelles } from '../../../shared/forms/options-depuis-libelles';
import {
  LIBELLES_ASSET_STATUS,
  LIBELLES_ASSET_TYPE,
  traduire,
} from '../../../shared/i18n/libelles';
import {
  ASSET_STATUSES,
  ASSET_TYPES,
  AssetResponse,
  AssetStatus,
  AssetType,
} from '../../../shared/models/asset.model';
import { ColonneTable, DataTable } from '../../../shared/ui/data-table/data-table';
import { EmptyState } from '../../../shared/ui/empty-state/empty-state';
import { ErrorMessage } from '../../../shared/ui/error-message/error-message';
import { OptionSelecteur, SelectField } from '../../../shared/ui/select-field/select-field';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { AssetStatusBadge } from '../../../shared/ui/status-badges/status-badges';
import { InventaireService } from './inventaire.service';

// Pas de `Button` importé : la navigation vers `/assets/nouveau` est un lien (« <a> pour une
// navigation », pas une action), donc de simples ancres `routerLink` stylées plutôt que
// `app-button`, qui ne rend qu'un `<button>` natif.

/**
 * Aucun pipe de date partagé n'existe encore dans `shared/` (limite déjà documentée dans
 * `features/assets/fiche/fiche.ts`, premier écran à en avoir eu besoin) : même formateur `Intl`
 * déterministe, dupliqué ici plutôt que d'introduire un utilitaire partagé pour deux occurrences
 * — à consolider dans `shared/` si la feature `tickets` en a besoin à son tour.
 */
const FORMATEUR_DATE = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'medium', timeZone: 'UTC' });

/**
 * E-01 — Inventaire des actifs.
 *
 * Filtrage et tri sont **côté client** (aucune pagination ni filtre serveur sur
 * `GET /api/v1/assets`) : l'état de filtre vit ici, dans l'écran, et non dans
 * `InventaireService` — ce dernier est un singleton racine partagé avec `Formulaire` (E-02, voir
 * son commentaire de tête), un état d'écran n'y a pas sa place.
 *
 * Une liste **filtrée** vide n'est pas traitée comme l'état « vide » du Lot 5 (réservé à un
 * inventaire réellement vide, `actifs().length === 0`) : elle est couverte par le message natif
 * de `app-data-table` (`messageVide`), qui reste dans l'état « contenu ».
 */
@Component({
  selector: 'app-assets-inventaire',
  imports: [
    AssetStatusBadge,
    DataTable,
    EmptyState,
    ErrorMessage,
    RouterLink,
    SelectField,
    Spinner,
  ],
  templateUrl: './inventaire.html',
  // Pas de `providers: [InventaireService]` ici : le service est `providedIn: 'root'`,
  // partagé à dessein avec `Formulaire` (E-02) — voir le commentaire de tête du service.
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Inventaire {
  private readonly etat = inject(InventaireService);

  protected readonly actifs = this.etat.actifs;
  protected readonly chargement = this.etat.chargement;
  protected readonly erreur = this.etat.erreur;

  protected recharger(): void {
    this.etat.recharger();
  }

  // --- Filtrage local (client) -------------------------------------------------------------

  protected readonly filtreType = new FormControl<AssetType | ''>('', { nonNullable: true });
  protected readonly filtreStatut = new FormControl<AssetStatus | ''>('', { nonNullable: true });

  private readonly valeurFiltreType = toSignal(this.filtreType.valueChanges, {
    initialValue: this.filtreType.value,
  });
  private readonly valeurFiltreStatut = toSignal(this.filtreStatut.valueChanges, {
    initialValue: this.filtreStatut.value,
  });

  protected readonly optionsFiltreType: readonly OptionSelecteur<AssetType | ''>[] = [
    { valeur: '', libelle: $localize`:@@assets.inventaire.filtreType.tous:Tous les types` },
    ...optionsDepuisLibelles(ASSET_TYPES, LIBELLES_ASSET_TYPE),
  ];

  protected readonly optionsFiltreStatut: readonly OptionSelecteur<AssetStatus | ''>[] = [
    { valeur: '', libelle: $localize`:@@assets.inventaire.filtreStatut.tous:Tous les états` },
    ...optionsDepuisLibelles(ASSET_STATUSES, LIBELLES_ASSET_STATUS),
  ];

  protected readonly actifsFiltres = computed(() => {
    const type = this.valeurFiltreType();
    const statut = this.valeurFiltreStatut();
    return this.actifs().filter(
      (actif) => (type === '' || actif.type === type) && (statut === '' || actif.status === statut),
    );
  });

  // --- Colonnes du tableau ------------------------------------------------------------------

  private readonly gabaritNom =
    viewChild.required<TemplateRef<{ $implicit: AssetResponse }>>('gabaritNom');
  private readonly gabaritStatut =
    viewChild.required<TemplateRef<{ $implicit: AssetResponse }>>('gabaritStatut');

  protected readonly colonnes = computed<readonly ColonneTable<AssetResponse>[]>(() => [
    {
      cle: 'nom',
      entete: this.libelleColonneNom,
      valeur: (actif) => actif.name,
      gabarit: this.gabaritNom(),
    },
    {
      cle: 'numeroSerie',
      entete: this.libelleColonneNumeroSerie,
      valeur: (actif) => actif.serialNumber,
    },
    {
      cle: 'type',
      entete: this.libelleColonneType,
      valeur: (actif) => traduire(LIBELLES_ASSET_TYPE, actif.type),
    },
    {
      cle: 'statut',
      entete: this.libelleColonneStatut,
      valeur: (actif) => traduire(LIBELLES_ASSET_STATUS, actif.status),
      gabarit: this.gabaritStatut(),
    },
    {
      cle: 'dateCreation',
      entete: this.libelleColonneDateCreation,
      valeur: (actif) => FORMATEUR_DATE.format(new Date(actif.createdAt)),
    },
  ]);

  protected readonly cleActif = (actif: AssetResponse): string => actif.id;

  // --- Textes localisés --------------------------------------------------------------------

  protected readonly libelleChargement = $localize`:@@assets.inventaire.chargement:Chargement de l'inventaire…`;
  protected readonly titreErreur = $localize`:@@assets.inventaire.erreur.titre:Chargement impossible`;
  protected readonly titreVide = $localize`:@@assets.inventaire.vide.titre:Aucun actif enregistré.`;
  protected readonly descriptionVide = $localize`:@@assets.inventaire.vide.description:Enregistrez le premier actif du parc.`;
  protected readonly messageAucunResultatFiltre = $localize`:@@assets.inventaire.filtre.aucunResultat:Aucun actif ne correspond à ces filtres.`;
  protected readonly legendeTable = $localize`:@@assets.inventaire.table.legende:Inventaire des actifs`;
  protected readonly libelleEnregistrer = $localize`:@@assets.inventaire.enregistrer:Enregistrer un actif`;
  protected readonly libelleFiltreType = $localize`:@@assets.inventaire.filtreType.label:Type`;
  protected readonly libelleFiltreStatut = $localize`:@@assets.inventaire.filtreStatut.label:État`;

  private readonly libelleColonneNom = $localize`:@@assets.inventaire.colonne.nom:Nom`;
  private readonly libelleColonneNumeroSerie = $localize`:@@assets.inventaire.colonne.numeroSerie:Numéro de série`;
  private readonly libelleColonneType = $localize`:@@assets.inventaire.colonne.type:Type`;
  private readonly libelleColonneStatut = $localize`:@@assets.inventaire.colonne.statut:État`;
  private readonly libelleColonneDateCreation = $localize`:@@assets.inventaire.colonne.dateCreation:Date de création`;
}
