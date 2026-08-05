import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * Enveloppe applicative : en-tête permanent et zone de contenu alimentée par le routeur.
 *
 * La navigation principale et l'habillage visuel relèvent du Lot 4 (design system) : ce shell
 * reste volontairement nu tant que les jetons de design et les composants de structure
 * n'existent pas.
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  /** Nom du produit, affiché dans l'en-tête. */
  protected readonly nomProduit = 'AssetFlow Core';
}
