import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ThemeService } from './core/theme/theme.service';
import { ThemePrefere, ThemeToggle } from './shared/ui/theme-toggle/theme-toggle';

/**
 * Enveloppe applicative : lien d'évitement, en-tête permanent avec la bascule de thème, et
 * zone de contenu alimentée par le routeur.
 *
 * C'est ici que se fait la jonction entre le composant de présentation `ThemeToggle`
 * (`shared/`) et le service qui détient la préférence (`core/`) : `shared/` n'a ainsi aucune
 * dépendance vers `core/`.
 */
@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, ThemeToggle],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly themes = inject(ThemeService);

  /** Nom du produit, affiché dans l'en-tête. */
  protected readonly nomProduit = 'AssetFlow Core';

  protected readonly themePrefere = this.themes.theme;

  protected changerTheme(theme: ThemePrefere): void {
    this.themes.definir(theme);
  }
}
