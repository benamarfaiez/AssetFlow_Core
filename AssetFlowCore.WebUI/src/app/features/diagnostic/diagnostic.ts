import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DiagnosticService } from './diagnostic.service';

/**
 * Écran de diagnostic du socle : il vérifie de bout en bout que l'application atteint l'API
 * (`GET /api/v1/assets`), que les erreurs sont normalisées et que la liaison temps réel s'établit.
 *
 * Ce n'est **pas** un écran produit : il n'implémente aucun des écrans `E-01`→`E-09` et sera
 * remplacé par l'inventaire au Lot 5. Il sert de preuve d'exécution du socle, exigée par les
 * critères du Lot 3.
 */
@Component({
  selector: 'app-diagnostic',
  templateUrl: './diagnostic.html',
  providers: [DiagnosticService],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Diagnostic {
  private readonly etat = inject(DiagnosticService);

  protected readonly actifs = this.etat.actifs;
  protected readonly chargement = this.etat.chargement;
  protected readonly erreur = this.etat.erreur;
  protected readonly nombreActifs = this.etat.nombreActifs;
  protected readonly statutTempsReel = this.etat.statutTempsReel;
  protected readonly dernierIncident = this.etat.dernierIncident;
  protected readonly erreurHub = this.etat.erreurHub;

  protected recharger(): void {
    this.etat.recharger();
  }

  protected connecterTempsReel(): void {
    void this.etat.connecterTempsReel();
  }
}
