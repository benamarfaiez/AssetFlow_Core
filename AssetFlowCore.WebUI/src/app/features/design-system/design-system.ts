import {
  ChangeDetectionStrategy,
  Component,
  TemplateRef,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { NonNullableFormBuilder, Validators } from '@angular/forms';
import { ASSET_STATUSES, AssetStatus, AssetType } from '../../shared/models/asset.model';
import { TICKET_CRITICALITIES, TICKET_STATUSES } from '../../shared/models/ticket.model';
import { Badge } from '../../shared/ui/badge/badge';
import { Breadcrumb } from '../../shared/ui/breadcrumb/breadcrumb';
import { Button } from '../../shared/ui/button/button';
import { Card } from '../../shared/ui/card/card';
import { CheckboxField } from '../../shared/ui/checkbox-field/checkbox-field';
import { ColonneTable, DataTable } from '../../shared/ui/data-table/data-table';
import { EmptyState } from '../../shared/ui/empty-state/empty-state';
import { ErrorMessage } from '../../shared/ui/error-message/error-message';
import { Modal } from '../../shared/ui/modal/modal';
import {
  NotificationList,
  NotificationUi,
} from '../../shared/ui/notification-list/notification-list';
import { OptionSelecteur, SelectField } from '../../shared/ui/select-field/select-field';
import { Spinner } from '../../shared/ui/spinner/spinner';
import {
  AssetStatusBadge,
  TicketCriticalityBadge,
  TicketStatusBadge,
} from '../../shared/ui/status-badges/status-badges';
import { TextField } from '../../shared/ui/text-field/text-field';
import { TextareaField } from '../../shared/ui/textarea-field/textarea-field';

interface ActifExemple {
  readonly id: string;
  readonly nom: string;
  readonly serie: string;
  readonly type: AssetType;
  readonly statut: AssetStatus;
}

const ACTIFS: readonly ActifExemple[] = [
  {
    id: 'a1',
    nom: 'Serveur de sauvegarde',
    serie: 'SRV-00042',
    type: 'Server',
    statut: 'InService',
  },
  { id: 'a2', nom: 'Portable RH', serie: 'LAP-00099', type: 'Laptop', statut: 'Down' },
  {
    id: 'a3',
    nom: 'Commutateur étage 3',
    serie: 'NET-00007',
    type: 'NetworkDevice',
    statut: 'InMaintenance',
  },
];

const OPTIONS_TYPE: readonly OptionSelecteur[] = [
  { valeur: 'Server', libelle: 'Serveur' },
  { valeur: 'Laptop', libelle: 'Ordinateur portable' },
  { valeur: 'NetworkDevice', libelle: 'Équipement réseau' },
];

/**
 * Page de revue du design system.
 *
 * Elle n'appartient à aucun parcours produit : elle rassemble tous les composants de `shared/ui`
 * sur un même écran, ce qui rend vérifiables **à l'œil et au clavier** les critères qu'aucune
 * commande ne peut trancher — rendu à 320 px de large, zoom à 200 %, contraste effectif dans les
 * deux thèmes, ordre de tabulation, restitution du focus après fermeture d'une modale.
 *
 * À retirer, comme `features/diagnostic`, lorsque les écrans du Lot 5 en tiendront lieu.
 */
@Component({
  selector: 'app-design-system',
  imports: [
    AssetStatusBadge,
    Badge,
    Breadcrumb,
    Button,
    Card,
    CheckboxField,
    DataTable,
    EmptyState,
    ErrorMessage,
    Modal,
    NotificationList,
    SelectField,
    Spinner,
    TextField,
    TextareaField,
    TicketCriticalityBadge,
    TicketStatusBadge,
  ],
  templateUrl: './design-system.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DesignSystem {
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly etatsActif = ASSET_STATUSES;
  protected readonly etatsIncident = TICKET_STATUSES;
  protected readonly criticites = TICKET_CRITICALITIES;
  protected readonly optionsType = OPTIONS_TYPE;

  protected readonly etapes = [
    { libelle: 'Inventaire', lien: '/design-system' },
    { libelle: 'Serveur de sauvegarde', lien: '/design-system' },
    { libelle: 'Design system' },
  ];

  protected readonly formulaire = this.fb.group({
    nom: this.fb.control('', [Validators.required, Validators.maxLength(150)]),
    serie: this.fb.control('', [Validators.required, Validators.minLength(5)]),
    type: this.fb.control('', [Validators.required]),
    description: this.fb.control('', [Validators.required]),
    confirmation: this.fb.control(false, [Validators.requiredTrue]),
  });

  protected readonly enCours = signal(false);
  protected readonly modaleOuverte = signal(false);
  protected readonly notifications = signal<readonly NotificationUi[]>([
    {
      id: 'n1',
      tonalite: 'succes',
      titre: 'Actif enregistré',
      message: 'SRV-00042 a été ajouté à l’inventaire.',
    },
  ]);

  protected readonly actifs = signal<readonly ActifExemple[]>(ACTIFS);
  protected readonly cleActif = (actif: ActifExemple): string => actif.id;

  private readonly gabaritStatut =
    viewChild.required<TemplateRef<{ $implicit: ActifExemple }>>('gabaritStatut');

  protected readonly colonnes = computed<readonly ColonneTable<ActifExemple>[]>(() => [
    { cle: 'nom', entete: 'Libellé', valeur: (actif) => actif.nom },
    { cle: 'serie', entete: 'Numéro de série', valeur: (actif) => actif.serie },
    {
      cle: 'statut',
      entete: 'État',
      valeur: (actif) => actif.statut,
      gabarit: this.gabaritStatut(),
    },
  ]);

  /** Marque tout le formulaire comme touché : ce que fait une soumission. */
  protected soumettre(): void {
    this.formulaire.markAllAsTouched();
  }

  /** Simule une action longue pour observer l'état occupé d'un bouton. */
  protected simulerActionLongue(): void {
    this.enCours.set(true);
    setTimeout(() => this.enCours.set(false), 1500);
  }

  protected viderTable(): void {
    this.actifs.set([]);
  }

  protected retablirTable(): void {
    this.actifs.set(ACTIFS);
  }

  protected ajouterNotification(): void {
    const identifiant = `n${this.notifications().length + 1}`;
    this.notifications.update((liste) => [
      ...liste,
      {
        id: identifiant,
        tonalite: 'danger',
        titre: 'Échec',
        message: 'Le transfert a été refusé par le serveur.',
      },
    ]);
  }

  protected rejeterNotification(identifiant: string): void {
    this.notifications.update((liste) => liste.filter((n) => n.id !== identifiant));
  }
}
