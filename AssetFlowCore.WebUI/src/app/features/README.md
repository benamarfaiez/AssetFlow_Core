# `features/` — écrans

Un dossier autonome par domaine fonctionnel, avec ses routes, son état et ses composants.
Propriétaire : `angular-feature-dev` (§13.1 du plan d'implémentation).

| Feature    | Écrans                                                   | Statut                                                       |
| ---------- | -------------------------------------------------------- | ------------------------------------------------------------ |
| `assets/`  | `E-01` inventaire · `E-02` formulaire · `E-03` fiche     | ✅ Lot 5.A                                                   |
| `tickets/` | `E-04` ouverture · `E-05` fiche · `E-06` file de travail | ✅ Lot 5.B (note d'assistance IA sur `E-05`, `E-08` : Lot 6) |
| `teams/`   | `E-07` administration                                    | ✅ Lot 5.C                                                   |

`diagnostic/` et `design-system/` (preuves d'exécution des Lots 3 et 4) ont été retirées une fois ces trois features livrées, comme prévu dès leur création.

Générer le squelette d'une feature avec le skill **`/scaffold-feature <nom>`**, jamais à la main.

## Règles

- `features/` → `shared/` + `core/`, et **aucun import croisé entre deux features** (vérifié par
  `npm run verifier:dependances`) : ce qui est partagé remonte dans `shared/`.
- Routes **chargées à la demande** : chaque feature exporte ses routes, `app.routes.ts` les
  référence par `loadChildren`.
- L'état vit dans un **service de feature** exposant des signaux en lecture seule ; le composant
  lit et déclenche, il ne calcule pas. Toute dérivation passe par `computed()` — un `effect()`
  qui écrit dans un signal pour dériver de l'état est un anti-patron.
- `ChangeDetectionStrategy.OnPush` sur chaque composant, `track` sur chaque `@for`, aucun
  abonnement RxJS non fermé (`takeUntilDestroyed()`).
- Quatre états gérés par écran : chargement, vide, erreur, contenu.
- Aucun `HttpClient` dans un composant : les appels passent par `core/api/`.

## Convention de nommage des fichiers

Le workspace suit le **guide de style 2025** du CLI, retenu par `ng new` : les composants ne
portent **pas** de suffixe (`app.ts` → classe `App`, `teams.ts` → classe `Teams`).
Les artefacts non composants portent leur rôle en suffixe : `*.service.ts`, `*.routes.ts`,
`*.interceptor.ts`, `*.model.ts`. Les gabarits du skill `/scaffold-feature` proposent
`*.component.ts` : **la convention du workspace prime**, comme le skill le prévoit lui-même.
