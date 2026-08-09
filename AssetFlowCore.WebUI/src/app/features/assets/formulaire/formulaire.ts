import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AssetsApiService } from '../../../core/api/assets-api.service';
import { focusPremierChampInvalide } from '../../../shared/forms/focus-invalide';
import { optionsDepuisLibelles } from '../../../shared/forms/options-depuis-libelles';
import { LIBELLES_ASSET_TYPE } from '../../../shared/i18n/libelles';
import { ApiError } from '../../../shared/models/api-error.model';
import { ASSET_TYPES, AssetType } from '../../../shared/models/asset.model';
import { Button } from '../../../shared/ui/button/button';
import { Card } from '../../../shared/ui/card/card';
import { ErrorMessage } from '../../../shared/ui/error-message/error-message';
import { SelectField } from '../../../shared/ui/select-field/select-field';
import { TextField } from '../../../shared/ui/text-field/text-field';
import { InventaireService } from '../inventaire/inventaire.service';

/**
 * E-02 — Formulaire d'enregistrement d'un actif (P-01).
 *
 * `POST /api/v1/assets` n'a **aucun validateur de surface côté API** : une seule erreur
 * `business` à la fois, sans dictionnaire `errors` (voir `gererErreur`). La validation locale
 * (RM-02, RM-04) est donc la seule protection contre un aller-retour réseau inutile ; RM-01
 * (unicité) et RM-03 (normalisation) ne peuvent, eux, être vérifiés que par le serveur.
 *
 * `InventaireService` (E-01) est fourni par la route parente `assets` (voir `assets.routes.ts`
 * et le commentaire de tête du service) : les deux écrans partagent la même instance malgré la
 * navigation entre routes sœurs, sans provider local à déclarer ici.
 */
@Component({
  selector: 'app-assets-formulaire',
  imports: [Button, Card, ErrorMessage, RouterLink, SelectField, TextField],
  templateUrl: './formulaire.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Formulaire {
  private readonly api = inject(AssetsApiService);
  private readonly inventaire = inject(InventaireService);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  private readonly elementFormulaire =
    viewChild.required<ElementRef<HTMLFormElement>>('elementFormulaire');

  protected readonly formulaire = this.fb.group({
    nom: this.fb.control('', [Validators.required, Validators.maxLength(100)]),
    numeroSerie: this.fb.control('', [
      Validators.required,
      Validators.minLength(5),
      Validators.maxLength(50),
    ]),
    type: this.fb.control<AssetType>('Server', { validators: [Validators.required] }),
  });

  protected readonly optionsType = optionsDepuisLibelles(ASSET_TYPES, LIBELLES_ASSET_TYPE);

  protected readonly enCours = signal(false);
  protected readonly erreurGlobale = signal<string | null>(null);

  protected readonly libelleTitre = $localize`:@@assets.formulaire.titre:Enregistrer un actif`;
  protected readonly libelleChampNom = $localize`:@@assets.formulaire.champNom:Libellé`;
  protected readonly libelleChampNumeroSerie = $localize`:@@assets.formulaire.champNumeroSerie:Numéro de série`;
  protected readonly libelleChampType = $localize`:@@assets.formulaire.champType:Type`;
  protected readonly libelleSoumettre = $localize`:@@assets.formulaire.soumettre:Enregistrer`;
  protected readonly libelleAnnuler = $localize`:@@assets.formulaire.annuler:Annuler`;

  private readonly messageLongueurNumeroSerie = $localize`:@@assets.formulaire.numeroSerie.longueur:Le numéro de série doit contenir entre 5 et 50 caractères.`;

  /** Même message imposé (§8) quel que soit le seuil en cause : la règle RM-02 n'en fait qu'une. */
  protected readonly messagesNumeroSerie: Readonly<Record<string, string>> = {
    minlength: this.messageLongueurNumeroSerie,
    maxlength: this.messageLongueurNumeroSerie,
  };

  /**
   * Reconnaît le refus de doublon de numéro de série (RM-01) au contenu du message, faute de
   * dictionnaire `errors` sur cette route : `AssetsApiService.register` ne peut renvoyer
   * qu'un message `business` déjà rédigé pour un humain par `errorInterceptor`, jamais un champ
   * nommé.
   */
  private static readonly MOTIF_DOUBLON = /num[ée]ro de s[ée]rie/i;

  protected soumettre(evenement: SubmitEvent): void {
    evenement.preventDefault();

    if (this.formulaire.invalid) {
      this.formulaire.markAllAsTouched();
      this.deplacerFocusVersChampInvalide();
      return;
    }

    this.erreurGlobale.set(null);
    this.enCours.set(true);

    const valeur = this.formulaire.getRawValue();

    // `takeUntilDestroyed` : sans lui, une navigation manuelle hors de cet écran pendant l'appel
    // laisserait l'abonnement actif — la réponse finirait par arriver et `router.navigate`
    // s'exécuterait quand même (le routeur est un singleton racine, indifférent au cycle de vie
    // de ce composant), renvoyant l'utilisateur vers `/assets` malgré sa navigation entre-temps.
    this.api
      .register({
        name: valeur.nom.trim(),
        serialNumber: valeur.numeroSerie.trim(),
        type: valeur.type,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (actifCree) => {
          this.enCours.set(false);
          // Critère P-01 : l'actif rejoint l'inventaire depuis le corps de la **réponse**
          // (numéro de série normalisé par l'API), jamais via un nouvel appel `GET`.
          this.inventaire.ajouterActif(actifCree);
          void this.router.navigate(['/assets']);
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
        $localize`:@@assets.formulaire.erreurInattendue:Une erreur inattendue est survenue.`,
      );
      return;
    }

    if (erreur.kind === 'business' && Formulaire.MOTIF_DOUBLON.test(erreur.message)) {
      // `premierMessageDeValidation` affiche déjà la clé `serveur` telle quelle : inutile de
      // passer `[messages]` pour ce cas précis.
      this.formulaire.controls.numeroSerie.setErrors({ serveur: erreur.message });
      this.deplacerFocusVersChampInvalide();
      return;
    }

    this.erreurGlobale.set(erreur.message);
  }

  /**
   * `aria-invalid` n'est écrit dans le DOM qu'au **prochain rendu** Angular, jamais de façon
   * synchrone avec `markAllAsTouched()`/`setErrors()` (zoneless : `suivreEtatControle` doit
   * d'abord provoquer ce rendu). Appeler `focusPremierChampInvalide` immédiatement après renverrait
   * donc toujours `false` — `afterNextRender` diffère l'appel jusqu'à ce que le gabarit soit à jour.
   */
  private deplacerFocusVersChampInvalide(): void {
    afterNextRender(() => focusPremierChampInvalide(this.elementFormulaire().nativeElement), {
      injector: this.injector,
    });
  }
}
