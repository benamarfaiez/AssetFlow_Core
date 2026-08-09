import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AssetsApiService } from '../../../core/api/assets-api.service';
import { TicketsApiService } from '../../../core/api/tickets-api.service';
import { focusPremierChampInvalide } from '../../../shared/forms/focus-invalide';
import { optionsDepuisLibelles } from '../../../shared/forms/options-depuis-libelles';
import { LIBELLES_TICKET_CRITICALITY } from '../../../shared/i18n/libelles';
import { ApiError } from '../../../shared/models/api-error.model';
import { AssetResponse } from '../../../shared/models/asset.model';
import { TICKET_CRITICALITIES, TicketCriticality } from '../../../shared/models/ticket.model';
import { Button } from '../../../shared/ui/button/button';
import { Card } from '../../../shared/ui/card/card';
import { ErrorMessage } from '../../../shared/ui/error-message/error-message';
import { OptionSelecteur, SelectField } from '../../../shared/ui/select-field/select-field';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { TextField } from '../../../shared/ui/text-field/text-field';
import { TextareaField } from '../../../shared/ui/textarea-field/textarea-field';

/**
 * Reconnaît le refus RM-12 (aucune équipe ne couvre le couple type × criticité — une anomalie de
 * **configuration du référentiel**, pas une erreur de saisie) au contenu du message : cette route
 * n'a pas de dictionnaire `errors`, donc pas d'autre moyen de la distinguer d'un refus ordinaire.
 */
const MOTIF_REFERENTIEL_INCOMPLET = /équipe/i;

/**
 * E-04 — Formulaire d'ouverture d'un incident (P-02).
 *
 * L'actif se sélectionne dans une liste, jamais en saisie libre (RM-08) : `assetId` pré-remplit
 * la sélection quand le formulaire est ouvert **depuis la fiche d'un actif**
 * (`/tickets/nouveau?assetId=...`, lié par `withComponentInputBinding()` — les query params sont
 * liés au même titre que les paramètres de route). La liste elle-même exclut les actifs déjà au
 * rebut (RM-09) : un actif qui le deviendrait entre le chargement et la soumission reste possible
 * (contrôlé par l'API, message générique affiché en tel cas).
 *
 * L'équipe assignée n'est **jamais** choisie ici (RM-11) : elle n'apparaît que dans la réponse
 * `201`, affichée en naviguant directement vers la fiche créée plutôt qu'en dupliquant son rendu
 * sur ce formulaire.
 */
@Component({
  selector: 'app-tickets-formulaire',
  imports: [Button, Card, ErrorMessage, RouterLink, SelectField, Spinner, TextField, TextareaField],
  templateUrl: './formulaire.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Formulaire {
  private readonly api = inject(TicketsApiService);
  private readonly assetsApi = inject(AssetsApiService);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  /** Pré-remplit la sélection d'actif ; absent quand on arrive depuis la file de travail. */
  readonly assetId = input<string>();

  private readonly elementFormulaire =
    viewChild.required<ElementRef<HTMLFormElement>>('elementFormulaire');

  private readonly ressourceActifs = rxResource<readonly AssetResponse[], void>({
    stream: () => this.assetsApi.getAll(),
    defaultValue: [],
  });

  protected readonly chargementActifs = this.ressourceActifs.isLoading;

  protected readonly erreurActifs = computed(() => {
    const erreur = this.ressourceActifs.error();
    return erreur instanceof ApiError ? erreur : null;
  });

  /** RM-09 : un actif déjà au rebut ne doit pas apparaître dans le sélecteur. */
  protected readonly optionsActifs = computed<readonly OptionSelecteur<string>[]>(() =>
    this.ressourceActifs
      .value()
      .filter((actif) => actif.status !== 'Decommissioned')
      .map((actif) => ({ valeur: actif.id, libelle: `${actif.name} (${actif.serialNumber})` })),
  );

  protected readonly optionsCriticite = optionsDepuisLibelles(
    TICKET_CRITICALITIES,
    LIBELLES_TICKET_CRITICALITY,
  );

  protected readonly formulaire = this.fb.group({
    assetId: this.fb.control('', [Validators.required]),
    title: this.fb.control('', [Validators.required, Validators.maxLength(150)]),
    description: this.fb.control('', [Validators.required]),
    criticality: this.fb.control<TicketCriticality | ''>('', [Validators.required]),
  });

  protected readonly enCours = signal(false);
  protected readonly erreurGlobale = signal<string | null>(null);
  protected readonly erreurReferentiel = signal<string | null>(null);

  protected readonly libelleTitre = $localize`:@@tickets.formulaire.titre:Ouvrir un incident`;
  protected readonly libelleChampActif = $localize`:@@tickets.formulaire.champActif:Équipement concerné`;
  protected readonly libelleChampTitre = $localize`:@@tickets.formulaire.champTitre:Titre`;
  protected readonly libelleChampDescription = $localize`:@@tickets.formulaire.champDescription:Description`;
  protected readonly libelleChampCriticite = $localize`:@@tickets.formulaire.champCriticite:Criticité`;
  protected readonly libelleSoumettre = $localize`:@@tickets.formulaire.soumettre:Ouvrir l'incident`;
  protected readonly libelleAnnuler = $localize`:@@tickets.formulaire.annuler:Annuler`;
  protected readonly avisAffectationAutomatique = $localize`:@@tickets.formulaire.affectationAutomatique:L'équipe en charge de cet incident sera déterminée automatiquement selon le type d'équipement et la criticité choisie.`;
  protected readonly messageErreurReferentiel = $localize`:@@tickets.formulaire.erreurReferentiel:La configuration des équipes ne couvre pas ce type d'équipement avec cette criticité. Contactez l'administrateur.`;
  protected readonly titreErreurReferentiel = $localize`:@@tickets.formulaire.erreurReferentiel.titre:Configuration du référentiel incomplète`;
  protected readonly libelleChargementActifs = $localize`:@@tickets.formulaire.chargementActifs:Chargement des équipements…`;
  protected readonly messageAucunActifDisponible = $localize`:@@tickets.formulaire.aucunActifDisponible:Aucun équipement disponible pour l'ouverture d'un incident.`;

  constructor() {
    // Effet de bord réel (pré-remplissage depuis un paramètre de route apparu après la première
    // résolution des actifs) : ni `computed()` ni un simple champ initial ne peuvent réagir à
    // l'arrivée asynchrone de `assetId` ou de la liste.
    afterNextRender(
      () => {
        const id = this.assetId();
        if (id !== undefined) {
          this.formulaire.controls.assetId.setValue(id);
        }
      },
      { injector: this.injector },
    );
  }

  protected soumettre(evenement: SubmitEvent): void {
    evenement.preventDefault();

    if (this.formulaire.invalid) {
      this.formulaire.markAllAsTouched();
      afterNextRender(() => focusPremierChampInvalide(this.elementFormulaire().nativeElement), {
        injector: this.injector,
      });
      return;
    }

    const valeur = this.formulaire.getRawValue();
    if (valeur.criticality === '') {
      return;
    }

    this.erreurGlobale.set(null);
    this.erreurReferentiel.set(null);
    this.enCours.set(true);

    this.api
      .create({
        assetId: valeur.assetId,
        title: valeur.title.trim(),
        description: valeur.description.trim(),
        criticality: valeur.criticality,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (incidentCree) => {
          this.enCours.set(false);
          // L'équipe assignée n'est affichée que sur la fiche : y naviguer directement satisfait
          // le critère « afficher l'équipe retenue après succès » sans le dupliquer ici.
          void this.router.navigate(['/tickets', incidentCree.id]);
        },
        error: (erreur: unknown) => {
          this.enCours.set(false);
          this.gererErreur(erreur);
        },
      });
  }

  private gererErreur(erreur: unknown): void {
    if (!(erreur instanceof ApiError)) {
      this.erreurGlobale.set(
        $localize`:@@tickets.formulaire.erreurInattendue:Une erreur inattendue est survenue.`,
      );
      return;
    }

    if (erreur.kind === 'business' && MOTIF_REFERENTIEL_INCOMPLET.test(erreur.message)) {
      this.erreurReferentiel.set(this.messageErreurReferentiel);
      return;
    }

    this.erreurGlobale.set(erreur.message);
  }
}
