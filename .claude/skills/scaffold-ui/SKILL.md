---
name: scaffold-ui
description: Génère un composant UI réutilisable du design system dans src/app/shared/components/ — entrées et sorties en Signals (input(), output()), styles alignés sur le framework CSS réellement installé (Tailwind, Angular Material ou SCSS), et attributs d'accessibilité, gestion du focus et du clavier par défaut. Utiliser quand l'utilisateur invoque /scaffold-ui, demande un composant partagé, un composant de présentation, un élément de design system, ou un composant accessible réutilisable.
user-invocable: true
allowed-tools:
  - Read
  - Write
  - Edit
  - Glob
  - Grep
  - PowerShell
  - Bash
---

# /scaffold-ui — Composant UI réutilisable (design system)

**Argument** : `[component-name]` en `kebab-case` (ex. `status-badge`, `confirm-dialog`, `data-table`).
Sans argument : demande le nom. Refuse un nom en `PascalCase`, avec espaces ou accents, et propose la forme `kebab-case`.

| Élément | Règle | Exemple pour `status-badge` |
|---|---|---|
| Dossier | `kebab-case` | `shared/components/status-badge/` |
| Classe | `PascalCase` + `Component` | `StatusBadgeComponent` |
| Sélecteur | `app-` + `kebab-case` | `app-status-badge` |

## Étape 1 — Détecter le terrain avant d'écrire

1. **Workspace Angular présent ?** Cherche `angular.json`. S'il est absent : **arrête-toi** et indique que la création du workspace relève de l'agent `angular-architect`. Ne scaffolde rien.
2. **Emplacement** : `src/app/shared/components/<nom>/` par défaut. Si le workspace utilise déjà `shared/ui/` (arborescence proposée par `angular-architect`) ou une autre convention, **aligne-toi sur l'existant** — ne crée jamais deux conventions concurrentes.
3. **Doublon** : si un composant équivalent existe déjà dans `shared/`, ne le duplique pas — signale-le et propose de l'étendre (nouvelle entrée, nouvelle variante) plutôt que d'en créer un second.
4. **Framework de style** — lis `package.json`, `angular.json` (tableau `styles`) et le fichier de styles global :

| Détecté | Conséquence |
|---|---|
| `tailwindcss` | classes utilitaires dans le template, **aucun fichier de styles** généré. Vérifie la version : v3 (`tailwind.config.js`) ou v4 (configuration CSS via `@import "tailwindcss"`) |
| `@angular/material` | compose les composants Material existants et utilise ses jetons de thème ; n'invente pas un style parallèle. Importe uniquement les composants standalone nécessaires |
| aucun des deux | fichier `.scss` local avec variables CSS (`var(--...)`) plutôt que des valeurs codées en dur |

**N'installe jamais un framework CSS de ta propre initiative** : si aucun n'est présent, génère la variante SCSS et signale le choix.

5. **`@angular/cdk` présent ?** (livré avec Material) Si oui, utilise `A11yModule` pour les cas avancés : `cdkTrapFocus` pour un dialogue, `FocusMonitor`, `LiveAnnouncer` pour les annonces vocales. Sinon, gère le focus à la main et dis-le.
6. **Runner de tests** : cible `test` d'`angular.json` + `devDependencies` → Vitest ou Jasmine/Karma.

## Étape 2 — Fichiers générés

| Fichier | Généré |
|---|---|
| `<nom>.component.ts` | toujours |
| `<nom>.component.html` | toujours (sauf convention de template inline dans le projet) |
| `<nom>.component.scss` | **uniquement** en variante SCSS — jamais de fichier de styles vide |
| `<nom>.component.spec.ts` | toujours, adapté au runner détecté |

Aucun `NgModule`, aucun barrel `index.ts`.

## Étape 3 — Contrat du composant

Un composant de `shared/` est **purement présentationnel**. Interdits :

- `HttpClient`, service d'API, `Router`, store de feature — **aucune** dépendance à `core/` ou `features/` ;
- logique métier : le composant reçoit des données prêtes à afficher et émet des intentions ;
- décorateurs `@Input` / `@Output`, `@HostBinding` / `@HostListener` (utilise `input()`, `output()` et les métadonnées `host`) ;
- `any`, `!important`, valeurs de couleur codées en dur hors jetons du thème.

Attendus :

- `input()` / `input.required()` / `output()` / `model()` pour le liaison bidirectionnelle ;
- `booleanAttribute` et `numberAttribute` en `transform` pour les entrées utilisables comme attributs HTML ;
- `ChangeDetectionStrategy.OnPush` ;
- toute valeur dérivée en `computed()` ; jamais d'appel de méthode coûteuse dans le template ;
- une entrée dédiée au **nom accessible** dès que le contenu visible ne suffit pas.

## Étape 4 — Gabarit

### `<nom>.component.ts`

```ts
import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
  output,
} from '@angular/core';

/**
 * Badge d'état réutilisable du design system.
 * Composant de présentation : aucune dépendance métier, aucun appel réseau.
 */
@Component({
  selector: 'app-status-badge',
  templateUrl: './status-badge.component.html',
  // styleUrl: './status-badge.component.scss',  // variante SCSS uniquement
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'inline-flex items-center gap-1',
  },
})
export class StatusBadgeComponent {
  /** Libellé affiché ; sert également de nom accessible. */
  readonly libelle = input.required<string>();

  /** Variante visuelle du badge. */
  readonly variante = input<'neutre' | 'succes' | 'alerte' | 'danger'>('neutre');

  /** Autorise le retrait du badge par l'utilisateur. */
  readonly supprimable = input(false, { transform: booleanAttribute });

  /** Émis lorsque l'utilisateur demande le retrait du badge. */
  readonly supprime = output<void>();

  protected readonly classesVariante = computed(() => `badge badge--${this.variante()}`);
}
```

### `<nom>.component.html`

```html
<span [class]="classesVariante()">{{ libelle() }}</span>

@if (supprimable()) {
  <button
    type="button"
    class="badge__retrait"
    [attr.aria-label]="'Retirer ' + libelle()"
    (click)="supprime.emit()"
  >
    <span aria-hidden="true">&times;</span>
  </button>
}
```

### `<nom>.component.spec.ts`

```ts
import { TestBed } from '@angular/core/testing';
import { StatusBadgeComponent } from './status-badge.component';

describe('StatusBadgeComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [StatusBadgeComponent] }).compileComponents();
  });

  it('devrait afficher le libellé fourni', () => {
    const fixture = TestBed.createComponent(StatusBadgeComponent);
    fixture.componentRef.setInput('libelle', 'En service');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('En service');
  });

  it('devrait exposer un nom accessible sur le bouton de retrait', () => {
    const fixture = TestBed.createComponent(StatusBadgeComponent);
    fixture.componentRef.setInput('libelle', 'En service');
    fixture.componentRef.setInput('supprimable', true);
    fixture.detectChanges();
    const bouton: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(bouton.getAttribute('aria-label')).toBe('Retirer En service');
  });
});
```

Sous **Vitest**, ajoute `import { beforeEach, describe, expect, it } from 'vitest';`. Les entrées signaux se pilotent avec `fixture.componentRef.setInput(...)`.

## Étape 5 — Accessibilité : règles appliquées par défaut

Ces points ne sont pas optionnels ; chaque composant généré doit les respecter ou documenter pourquoi il s'en écarte.

**Sémantique d'abord**
- Utilise l'élément natif porteur du comportement : `<button type="button">` pour une action, `<a>` pour une navigation, `<input>`/`<label>` pour une saisie. Un `<div role="button">` n'est admis qu'en dernier recours, et exige alors `tabindex="0"` **et** la gestion clavier complète.
- N'ajoute un `role` que si la sémantique native est indisponible. Pas de rôle redondant (`<button role="button">`).

**Nom accessible**
- Tout contrôle a un nom : contenu textuel visible, sinon `aria-label` / `aria-labelledby` alimenté par une entrée.
- Les glyphes et icônes décoratifs portent `aria-hidden="true"` ; les images signifiantes ont un `alt` pertinent.

**Clavier**
- Ordre de tabulation naturel ; **jamais de `tabindex` positif**.
- `Enter` et `Space` activent un contrôle personnalisé ; `Escape` referme un élément transitoire (menu, dialogue, panneau).
- Composants composites (onglets, listbox, menu) : navigation par flèches et un seul point d'entrée dans l'ordre de tabulation.

**Focus**
- Focus visible dans tous les cas : ne supprime jamais l'anneau de focus sans le remplacer (`:focus-visible`).
- Élément transitoire : focus déplacé à l'ouverture, **restitué à l'élément déclencheur** à la fermeture, focus piégé pendant l'ouverture (`cdkTrapFocus` si `@angular/cdk` est présent).

**États**
- Un bouton `disabled` n'est pas focusable : dans une barre d'outils ou un menu, préfère `aria-disabled="true"` avec le contrôle laissé focusable.
- Expose l'état aux technologies d'assistance : `aria-expanded`, `aria-selected`, `aria-current`, `aria-invalid` + `aria-describedby` pour un message d'erreur.
- Les changements asynchrones significatifs passent par une région `aria-live="polite"` (ou `LiveAnnouncer`).

**Visuel**
- Contraste minimal 4,5:1 pour le texte, 3:1 pour les éléments d'interface ; l'information n'est **jamais portée par la seule couleur** (ajoute un libellé, une icône ou une forme).
- Cible tactile d'au moins 44 × 44 px pour les contrôles interactifs.
- Respecte `@media (prefers-reduced-motion: reduce)` pour toute animation.

## Étape 6 — Vérifier

Exécute et **rapporte la sortie réelle** :

```powershell
npx ng build
npx ng test --watch=false
```

Puis parcours cette liste de contrôle sur le composant généré, et signale chaque point non satisfait :

1. Le composant est-il utilisable **au clavier seul**, du premier au dernier contrôle ?
2. Chaque contrôle a-t-il un nom accessible non vide ?
3. Le focus reste-t-il visible et prévisible ?
4. L'information est-elle compréhensible sans la couleur ?
5. Le composant est-il exempt de toute dépendance à `core/` et `features/` ?

N'affirme jamais qu'un composant est accessible sans avoir passé cette liste ; n'affirme jamais qu'il compile sans exécution à l'appui.

## Étape 7 — Rapport final

1. **Fichiers créés** (chemins).
2. **Choix effectués** : emplacement retenu, framework de style détecté et version, runner de tests, `@angular/cdk` disponible ou non.
3. **API publique du composant** : tableau des entrées (nom, type, valeur par défaut, obligatoire) et des sorties.
4. **Accessibilité** : points de la liste de contrôle satisfaits, et ceux qui restent à traiter (avec la raison).
5. **Vérification** : commandes lancées et résultat réel.
6. **Exemple d'utilisation** : un extrait de template montrant l'import et l'usage du composant.

## Rappels de périmètre

- Ce skill produit un composant **de présentation réutilisable**. L'intégration dans un écran et la logique métier relèvent de l'agent `angular-feature-dev`.
- Les types du contrat d'API viennent de `shared/models/` (skill `/sync-api-dtos`) : un composant de `shared/` peut les **typer en entrée**, mais ne les récupère jamais lui-même.
- Aucune modification du backend .NET, ni de la configuration globale du workspace (`angular.json`, `app.config.ts`) : signale les besoins au lieu de les appliquer.
