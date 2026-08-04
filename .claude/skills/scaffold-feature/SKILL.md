---
name: scaffold-feature
description: Génère l'arborescence complète d'une fonctionnalité Angular moderne dans src/app/features/<nom>/ — routes en lazy loading, composant standalone à Signals avec syntaxe @if/@for, service d'état basé sur Signals (ou NgRx SignalStore si disponible) et squelette de test unitaire. Utiliser quand l'utilisateur invoque /scaffold-feature, demande de créer une nouvelle feature Angular, d'initialiser un écran ou de générer le squelette d'un module fonctionnel frontend.
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

# /scaffold-feature — Génération d'une fonctionnalité Angular

**Argument** : `[feature-name]` en `kebab-case` (ex. `assets`, `ticket-detail`, `dashboard`).
Sans argument : demande le nom. Refuse un nom en `PascalCase`, avec espaces ou accents, et propose la forme `kebab-case` correspondante.

Dérivations de nom à appliquer partout :

| Élément | Règle | Exemple pour `ticket-detail` |
|---|---|---|
| Dossier et fichiers | `kebab-case` | `features/ticket-detail/` |
| Classe de composant | `PascalCase` + `Component` | `TicketDetailComponent` |
| Classe de service | `PascalCase` + `Service` (ou `Store` pour un SignalStore) | `TicketDetailService` |
| Constante de routes | `SCREAMING_SNAKE_CASE` + `_ROUTES` | `TICKET_DETAIL_ROUTES` |
| Sélecteur | `app-` + `kebab-case` | `app-ticket-detail` |

## Étape 1 — Vérifier le terrain

1. **Workspace Angular présent ?** Cherche `angular.json`. S'il est absent : **arrête-toi**, dis-le, et indique que la création du workspace relève de l'agent `angular-architect`. Ne scaffolde rien.
2. **La feature existe-t-elle déjà ?** Si `features/<nom>/` existe, n'écrase rien : liste le contenu, signale les fichiers manquants du jeu attendu et demande s'il faut les compléter.
3. **Relever les conventions en place** avant d'écrire, en lisant une feature existante :
   - suffixes de fichiers (`x.component.ts` ou `x.ts` — depuis Angular 20, les schematics du CLI omettent le suffixe par défaut ; vérifie avec `npx ng g c --help` et **aligne-toi sur l'existant**, qui prime sur le défaut du CLI et sur les noms proposés ici) ;
   - template externe (`.html`) ou inline ; feuille de style présente ou non ;
   - dossier des types (`shared/models/` ou `core/models/`) et des services d'API (`core/api/`) ;
   - runner de tests : lis la cible `test` dans `angular.json` et les `devDependencies` de `package.json` pour choisir entre **Vitest** et **Jasmine/Karma**.
4. **État global** : si `@ngrx/signals` figure dans `package.json`, tu peux générer un SignalStore. Sinon, **Signals natifs** — n'ajoute jamais la dépendance de ta propre initiative (la version stable 21.x est incompatible avec Angular 22, seule une préversion l'accompagne).

## Étape 2 — Fichiers générés

| Fichier | Rôle |
|---|---|
| `<nom>.routes.ts` | routes de la feature, chargées en lazy |
| `<nom>.component.ts` | composant standalone (Signals, `OnPush`, `inject()`) |
| `<nom>.component.html` | template en syntaxe `@if` / `@for` — généré **en plus** de la liste minimale, sauf si la convention du projet est le template inline |
| `<nom>.service.ts` | état de la feature en Signals (ou SignalStore) |
| `<nom>.component.spec.ts` | squelette de test adapté au runner détecté |

Aucun `NgModule`, aucun fichier de barrel `index.ts`, aucune feuille de style vide.

## Étape 3 — Gabarits

### `<nom>.routes.ts`

```ts
import { Routes } from '@angular/router';

/** Routes de la fonctionnalité « ticket-detail », chargées à la demande. */
export const TICKET_DETAIL_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./ticket-detail.component').then((m) => m.TicketDetailComponent),
    title: 'Détail du ticket',
  },
];
```

### `<nom>.component.ts`

```ts
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TicketDetailService } from './ticket-detail.service';

@Component({
  selector: 'app-ticket-detail',
  templateUrl: './ticket-detail.component.html',
  providers: [TicketDetailService],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TicketDetailComponent {
  private readonly etat = inject(TicketDetailService);

  // Signaux exposés au template en lecture seule
  protected readonly elements = this.etat.elements;
  protected readonly chargement = this.etat.chargement;
  protected readonly erreur = this.etat.erreur;
  protected readonly nombre = this.etat.nombre;

  constructor() {
    this.etat.charger();
  }
}
```

`standalone: true` n'est pas déclaré : c'est le comportement par défaut depuis Angular 19. Le service est fourni au niveau du composant (`providers`) pour un état propre à l'écran ; utilise `providedIn: 'root'` uniquement si l'état doit survivre à la navigation.

### `<nom>.component.html`

```html
@if (chargement()) {
  <p>Chargement en cours…</p>
} @else if (erreur()) {
  <p role="alert">{{ erreur() }}</p>
} @else {
  <p>{{ nombre() }} élément(s)</p>
  <ul>
    @for (element of elements(); track element.id) {
      <li>{{ element.name }}</li>
    } @empty {
      <li>Aucun élément à afficher.</li>
    }
  </ul>
}
```

`@for` porte toujours un `track` sur un identifiant stable, et un bloc `@empty`. Jamais de `*ngIf` / `*ngFor`.

### `<nom>.service.ts` — Signals natifs (défaut)

```ts
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

/**
 * État local de la fonctionnalité « ticket-detail ».
 * Les appels HTTP passent par les services de `core/api/`, jamais par HttpClient directement.
 */
@Injectable()
export class TicketDetailService {
  private readonly api = inject(/* TicketsApiService */);
  private readonly destroyRef = inject(DestroyRef);

  // Sources d'état privées, mutables
  private readonly _elements = signal<readonly unknown[]>([]);
  private readonly _chargement = signal(false);
  private readonly _erreur = signal<string | null>(null);

  // Surface publique en lecture seule
  readonly elements = this._elements.asReadonly();
  readonly chargement = this._chargement.asReadonly();
  readonly erreur = this._erreur.asReadonly();
  readonly nombre = computed(() => this._elements().length);

  charger(): void {
    this._chargement.set(true);
    this._erreur.set(null);

    this.api
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (donnees) => {
          this._elements.set(donnees);
          this._chargement.set(false);
        },
        error: () => {
          this._erreur.set('Le chargement des données a échoué.');
          this._chargement.set(false);
        },
      });
  }
}
```

Règles de l'état :

- Toute valeur dérivée passe par `computed()`. **Un `effect()` qui écrit dans un signal pour dériver de l'état est un anti-pattern** — `effect()` est réservé aux effets de bord réels.
- `linkedSignal()` pour un état qui doit se réinitialiser au changement d'une source (sélection remise à zéro quand un filtre change).
- Aucun `BehaviorSubject` utilisé comme store. RxJS reste cantonné au flux HTTP.
- Alternative acceptable si l'écran n'a besoin que d'un chargement simple : `httpResource()` / `rxResource()`, qui fournit `value`, `isLoading` et `error` sans état manuel. Annonce le choix dans le rapport.

### `<nom>.service.ts` — variante SignalStore (si `@ngrx/signals` est installé)

```ts
import { signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { computed, inject } from '@angular/core';

type TicketDetailEtat = {
  elements: readonly unknown[];
  chargement: boolean;
  erreur: string | null;
};

export const TicketDetailStore = signalStore(
  withState<TicketDetailEtat>({ elements: [], chargement: false, erreur: null }),
  withComputed(({ elements }) => ({ nombre: computed(() => elements().length) })),
  withMethods((store) => ({
    /* charger(), reinitialiser(), ... */
  })),
);
```

### `<nom>.component.spec.ts`

Squelette **Jasmine / Karma** :

```ts
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TicketDetailComponent } from './ticket-detail.component';

describe('TicketDetailComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TicketDetailComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('devrait se créer', () => {
    const fixture = TestBed.createComponent(TicketDetailComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it("devrait afficher l'état vide quand aucun élément n'est chargé", () => {
    const fixture = TestBed.createComponent(TicketDetailComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Aucun élément');
  });
});
```

Pour **Vitest**, le corps est identique ; ajoute l'import explicite des primitives :

```ts
import { beforeEach, describe, expect, it } from 'vitest';
```

`provideHttpClientTesting()` doit toujours suivre `provideHttpClient()`.

## Étape 4 — Brancher la route racine

Ajoute l'entrée lazy dans `app.routes.ts`, **en respectant le style du fichier** :

```ts
{
  path: 'ticket-detail',
  loadChildren: () => import('./features/ticket-detail/ticket-detail.routes').then((m) => m.TICKET_DETAIL_ROUTES),
}
```

Si `app.routes.ts` est absent, ne le crée pas : signale-le comme relevant de `angular-architect`. Mentionne toujours cette modification dans le rapport — c'est le seul fichier hors de la feature que ce skill touche.

## Étape 5 — Vérifier

Exécute et **rapporte la sortie réelle** :

```powershell
npx ng build
npx ng test --watch=false
```

N'annonce jamais que la feature est fonctionnelle sans exécution à l'appui. Si les dépendances ne sont pas installées ou si l'échec est indépendant des fichiers générés, dis-le explicitement.

## Étape 6 — Rapport final

1. **Fichiers créés** (chemins) et route racine ajoutée.
2. **Choix effectués** : convention de nommage retenue (et pourquoi elle diffère éventuellement du défaut du CLI), Signals natifs ou SignalStore, template externe ou inline, runner de tests détecté.
3. **Vérification** : commandes lancées et résultat réel.
4. **À compléter** : les emplacements laissés en `TODO` — service d'API à injecter, type réel des éléments (le gabarit utilise `unknown` volontairement, à remplacer par le type de `shared/models/`), libellés et présentation.
5. **Dépendances externes** : composants de `shared/` réutilisables ou manquants, endpoints backend nécessaires et absents.

## Rappels de périmètre

- Le gabarit livre un **squelette fonctionnel typé**, pas un écran fini : la logique métier et la présentation relèvent de l'agent `angular-feature-dev`.
- Les types du contrat d'API et les services `core/api/` sont produits par le skill `/sync-api-dtos` — ne les redéfinis pas dans la feature.
- Les composants réutilisables vont dans `shared/`, jamais dupliqués dans la feature.
- Aucune modification du backend .NET.
